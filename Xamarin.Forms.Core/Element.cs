using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml;
using Xamarin.Forms.Internals;

namespace Xamarin.Forms
{
	public abstract class Element : BindableObject, IElement, INameScope, IElementController
	{
		internal static readonly ReadOnlyCollection<Element> EmptyChildren = new ReadOnlyCollection<Element>(new Element[0]);

		public static readonly BindableProperty ClassIdProperty = BindableProperty.Create("ClassId", typeof(string), typeof(View), null);

		string _automationId;

		List<Action<object, ResourcesChangedEventArgs>> _changeHandlers;

		List<KeyValuePair<string, BindableProperty>> _dynamicResources;

		IEffectControlProvider _effectControlProvider;

		TrackableCollection<Effect> _effects;

		Guid? _id;

		Element _parentOverride;

		IPlatform _platform;

		string _styleId;

		public string AutomationId
		{
			get { return _automationId; }
			set
			{
				if (_automationId != null)
					throw new InvalidOperationException("AutomationId may only be set one time");
				_automationId = value;
			}
		}

		public string ClassId
		{
			get { return (string)GetValue(ClassIdProperty); }
			set { SetValue(ClassIdProperty, value); }
		}

		public IList<Effect> Effects
		{
			get
			{
				if (_effects == null)
				{
					_effects = new TrackableCollection<Effect>();
					_effects.CollectionChanged += EffectsOnCollectionChanged;
					_effects.Clearing += EffectsOnClearing;
				}
				return _effects;
			}
		}

		public Guid Id
		{
			get
			{
				if (!_id.HasValue)
					_id = Guid.NewGuid();
				return _id.Value;
			}
		}

		[Obsolete("Use Parent")]
		public VisualElement ParentView
		{
			get
			{
				Element parent = Parent;
				while (parent != null)
				{
					var parentView = parent as VisualElement;
					if (parentView != null)
						return parentView;
					parent = parent.RealParent;
				}
				return null;
			}
		}

		public string StyleId
		{
			get { return _styleId; }
			set
			{
				if (_styleId == value)
					return;

				OnPropertyChanging();
				_styleId = value;
				OnPropertyChanged();
			}
		}

		internal virtual ReadOnlyCollection<Element> LogicalChildren
		{
			get { return EmptyChildren; }
		}

		internal bool Owned { get; set; }

		internal Element ParentOverride
		{
			get { return _parentOverride; }
			set
			{
				if (_parentOverride == value)
					return;

				bool emitChange = Parent != value;

				if (emitChange)
					OnPropertyChanging(nameof(Parent));

				_parentOverride = value;

				if (emitChange)
					OnPropertyChanged(nameof(Parent));
			}
		}

		internal IPlatform Platform
		{
			get
			{
				if (_platform == null && RealParent != null)
					return RealParent.Platform;
				return _platform;
			}
			set
			{
				if (_platform == value)
					return;
				_platform = value;
				if (PlatformSet != null)
					PlatformSet(this, EventArgs.Empty);
				foreach (Element descendant in Descendants())
				{
					descendant._platform = _platform;
					if (descendant.PlatformSet != null)
						descendant.PlatformSet(this, EventArgs.Empty);
				}
			}
		}

		// you're not my real dad
		internal Element RealParent { get; private set; }

		List<KeyValuePair<string, BindableProperty>> DynamicResources
		{
			get { return _dynamicResources ?? (_dynamicResources = new List<KeyValuePair<string, BindableProperty>>(4)); }
		}

		void IElement.AddResourcesChangedListener(Action<object, ResourcesChangedEventArgs> onchanged)
		{
			_changeHandlers = _changeHandlers ?? new List<Action<object, ResourcesChangedEventArgs>>(2);
			_changeHandlers.Add(onchanged);
		}

		public Element Parent
		{
			get { return _parentOverride ?? RealParent; }
			set
			{
				if (RealParent == value)
					return;

				OnPropertyChanging();

				if (RealParent != null)
					((IElement)RealParent).RemoveResourcesChangedListener(OnParentResourcesChanged);
				RealParent = value;
				if (RealParent != null)
				{
					OnParentResourcesChanged(RealParent.GetMergedResources());
					((IElement)RealParent).AddResourcesChangedListener(OnParentResourcesChanged);
				}

				object context = value != null ? value.BindingContext : null;
				if (value != null)
				{
					value.SetChildInheritedBindingContext(this, context);
				}
				else
				{
					SetInheritedBindingContext(this, null);
				}

				OnParentSet();

				if (RealParent != null)
				{
					IPlatform platform = RealParent.Platform;
					if (platform != null)
						Platform = platform;
				}

				OnPropertyChanged();
			}
		}

		void IElement.RemoveResourcesChangedListener(Action<object, ResourcesChangedEventArgs> onchanged)
		{
			if (_changeHandlers == null)
				return;
			_changeHandlers.Remove(onchanged);
		}

		IEffectControlProvider IElementController.EffectControlProvider
		{
			get { return _effectControlProvider; }
			set
			{
				if (_effectControlProvider == value)
					return;
				if (_effectControlProvider != null && _effects != null)
				{
					foreach (Effect effect in _effects)
						effect?.SendDetached();
				}
				_effectControlProvider = value;
				if (_effectControlProvider != null && _effects != null)
				{
					foreach (Effect effect in _effects)
					{
						if (effect != null)
							AttachEffect(effect);
					}
				}
			}
		}

		void IElementController.SetValueFromRenderer(BindableProperty property, object value)
		{
			SetValueCore(property, value);
		}

		void IElementController.SetValueFromRenderer(BindablePropertyKey property, object value)
		{
			SetValueCore(property, value);
		}

		object INameScope.FindByName(string name)
		{
			INameScope namescope = GetNameScope();
			if (namescope == null)
				throw new InvalidOperationException("this element is not in a namescope");
			return namescope.FindByName(name);
		}

		void INameScope.RegisterName(string name, object scopedElement)
		{
			INameScope namescope = GetNameScope();
			if (namescope == null)
				throw new InvalidOperationException("this element is not in a namescope");
			namescope.RegisterName(name, scopedElement);
		}

		void INameScope.RegisterName(string name, object scopedElement, IXmlLineInfo xmlLineInfo)
		{
			INameScope namescope = GetNameScope();
			if (namescope == null)
				throw new InvalidOperationException("this element is not in a namescope");
			namescope.RegisterName(name, scopedElement, xmlLineInfo);
		}

		void INameScope.UnregisterName(string name)
		{
			INameScope namescope = GetNameScope();
			if (namescope == null)
				throw new InvalidOperationException("this element is not in a namescope");
			namescope.UnregisterName(name);
		}

		public event EventHandler<ElementEventArgs> ChildAdded;

		public event EventHandler<ElementEventArgs> ChildRemoved;

		public event EventHandler<ElementEventArgs> DescendantAdded;

		public event EventHandler<ElementEventArgs> DescendantRemoved;

		public new void RemoveDynamicResource(BindableProperty property)
		{
			base.RemoveDynamicResource(property);
		}

		public new void SetDynamicResource(BindableProperty property, string key)
		{
			base.SetDynamicResource(property, key);
		}

		protected override void OnBindingContextChanged()
		{
			var gotBindingContext = false;
			object bc = null;

			for (var index = 0; index < LogicalChildren.Count; index++)
			{
				Element child = LogicalChildren[index];

				if (!gotBindingContext)
				{
					bc = BindingContext;
					gotBindingContext = true;
				}

				SetChildInheritedBindingContext(child, bc);
			}

			base.OnBindingContextChanged();
		}

		protected virtual void OnChildAdded(Element child)
		{
			child.Parent = this;
			if (Platform != null)
				child.Platform = Platform;

			child.ApplyBindings();

			if (ChildAdded != null)
				ChildAdded(this, new ElementEventArgs(child));

			OnDescendantAdded(child);
			foreach (Element element in child.Descendants())
				OnDescendantAdded(element);
		}

		protected virtual void OnChildRemoved(Element child)
		{
			child.Parent = null;

			if (ChildRemoved != null)
				ChildRemoved(child, new ElementEventArgs(child));

			OnDescendantRemoved(child);
			foreach (Element element in child.Descendants())
				OnDescendantRemoved(element);
		}

		protected virtual void OnParentSet()
		{
			ParentSet?.Invoke(this, EventArgs.Empty);
		}

		protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			base.OnPropertyChanged(propertyName);

			if (_effects == null || _effects.Count == 0)
				return;

			var args = new PropertyChangedEventArgs(propertyName);
			foreach (Effect effect in _effects)
			{
				effect?.SendOnElementPropertyChanged(args);
			}
		}

		internal IEnumerable<Element> Descendants()
		{
			var queue = new Queue<Element>(16);
			queue.Enqueue(this);

			while (queue.Count > 0)
			{
				ReadOnlyCollection<Element> children = queue.Dequeue().LogicalChildren;
				for (var i = 0; i < children.Count; i++)
				{
					Element child = children[i];
					yield return child;
					queue.Enqueue(child);
				}
			}
		}

		internal void OnParentResourcesChanged(object sender, ResourcesChangedEventArgs e)
		{
			OnParentResourcesChanged(e.Values);
		}

		internal virtual void OnParentResourcesChanged(IEnumerable<KeyValuePair<string, object>> values)
		{
			OnResourcesChanged(values);
		}

		internal override void OnRemoveDynamicResource(BindableProperty property)
		{
			DynamicResources.RemoveAll(kvp => kvp.Value == property);
			if (DynamicResources.Count == 0)
				_dynamicResources = null;
			base.OnRemoveDynamicResource(property);
		}

		internal void OnResourcesChanged(object sender, ResourcesChangedEventArgs e)
		{
			OnResourcesChanged(e.Values);
		}

		internal void OnResourcesChanged(IEnumerable<KeyValuePair<string, object>> values)
		{
			if (values == null)
				return;
			if (_changeHandlers != null)
				foreach (Action<object, ResourcesChangedEventArgs> handler in _changeHandlers)
					handler(this, new ResourcesChangedEventArgs(values));
			if (_dynamicResources == null)
				return;
			foreach (KeyValuePair<string, object> value in values)
			{
				List<BindableProperty> changedResources = null;
				foreach (KeyValuePair<string, BindableProperty> dynR in DynamicResources)
				{
					if (dynR.Key != value.Key)
						continue;
					changedResources = changedResources ?? new List<BindableProperty>();
					changedResources.Add(dynR.Value);
				}
				if (changedResources == null)
					continue;
				foreach (BindableProperty changedResource in changedResources)
					OnResourceChanged(changedResource, value.Value);
			}
		}

		internal override void OnSetDynamicResource(BindableProperty property, string key)
		{
			base.OnSetDynamicResource(property, key);
			DynamicResources.Add(new KeyValuePair<string, BindableProperty>(key, property));
			object value;
			if (this.TryGetResource(key, out value))
				OnResourceChanged(property, value);
		}

		internal event EventHandler ParentSet;

		internal event EventHandler PlatformSet;

		internal virtual void SetChildInheritedBindingContext(Element child, object context)
		{
			SetInheritedBindingContext(child, context);
		}

		internal IEnumerable<Element> VisibleDescendants()
		{
			var queue = new Queue<Element>(16);
			queue.Enqueue(this);

			while (queue.Count > 0)
			{
				ReadOnlyCollection<Element> children = queue.Dequeue().LogicalChildren;
				for (var i = 0; i < children.Count; i++)
				{
					var child = children[i] as VisualElement;
					if (child == null || !child.IsVisible)
						continue;
					yield return child;
					queue.Enqueue(child);
				}
			}
		}

		void AttachEffect(Effect effect)
		{
			if (_effectControlProvider == null)
				return;
			if (effect.IsAttached)
				throw new InvalidOperationException("Cannot attach Effect to multiple sources");

			Effect effectToRegister = effect;
			if (effect is RoutingEffect)
				effectToRegister = ((RoutingEffect)effect).Inner;
			_effectControlProvider.RegisterEffect(effectToRegister);
			effectToRegister.Element = this;
			effect.SendAttached();
		}

		void EffectsOnClearing(object sender, EventArgs eventArgs)
		{
			foreach (Effect effect in _effects)
			{
				effect.ClearEffect();
			}
		}

		void EffectsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					foreach (Effect effect in e.NewItems)
					{
						AttachEffect(effect);
					}
					break;
				case NotifyCollectionChangedAction.Move:
					break;
				case NotifyCollectionChangedAction.Remove:
					foreach (Effect effect in e.OldItems)
					{
						effect.ClearEffect();
					}
					break;
				case NotifyCollectionChangedAction.Replace:
					foreach (Effect effect in e.NewItems)
					{
						AttachEffect(effect);
					}
					foreach (Effect effect in e.OldItems)
					{
						effect.ClearEffect();
					}
					break;
				case NotifyCollectionChangedAction.Reset:
					if (e.NewItems != null)
					{
						foreach (Effect effect in e.NewItems)
						{
							AttachEffect(effect);
						}
					}
					if (e.OldItems != null)
					{
						foreach (Effect effect in e.OldItems)
						{
							effect.ClearEffect();
						}
					}
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		INameScope GetNameScope()
		{
			INameScope namescope = NameScope.GetNameScope(this);
			Element p = RealParent;
			while (namescope == null && p != null)
			{
				namescope = NameScope.GetNameScope(p);
				p = p.RealParent;
			}
			return namescope;
		}

		void OnDescendantAdded(Element child)
		{
			if (DescendantAdded != null)
				DescendantAdded(this, new ElementEventArgs(child));

			if (RealParent != null)
				RealParent.OnDescendantAdded(child);
		}

		void OnDescendantRemoved(Element child)
		{
			if (DescendantRemoved != null)
				DescendantRemoved(this, new ElementEventArgs(child));

			if (RealParent != null)
				RealParent.OnDescendantRemoved(child);
		}

		void OnResourceChanged(BindableProperty property, object value)
		{
			SetValueCore(property, value, SetValueFlags.ClearOneWayBindings | SetValueFlags.ClearTwoWayBindings);
		}
	}
}