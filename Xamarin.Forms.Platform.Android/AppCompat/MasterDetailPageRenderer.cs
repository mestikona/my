using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Android.Support.V4.Widget;
using Android.Views;

namespace Xamarin.Forms.Platform.Android.AppCompat
{
	public class MasterDetailPageRenderer : DrawerLayout, IVisualElementRenderer, DrawerLayout.IDrawerListener
	{
		#region Statics

		//from Android source code
		const uint DefaultScrimColor = 0x99000000;

		#endregion

		int _currentLockMode = -1;
		MasterDetailContainer _detailLayout;

		bool _disposed;
		bool _isPresentingFromCore;
		MasterDetailContainer _masterLayout;
		bool _presented;
		VisualElementTracker _tracker;

		public MasterDetailPageRenderer() : base(Forms.Context)
		{
		}

		MasterDetailPage Element { get; set; }

		bool Presented
		{
			get { return _presented; }
			set
			{
				if (value == _presented)
					return;
				UpdateSplitViewLayout();
				_presented = value;
				if (Element.MasterBehavior == MasterBehavior.Default && Element.ShouldShowSplitMode)
					return;
				if (_presented)
					OpenDrawer(_masterLayout);
				else
					CloseDrawer(_masterLayout);
			}
		}

		void IDrawerListener.OnDrawerClosed(global::Android.Views.View drawerView)
		{
		}

		void IDrawerListener.OnDrawerOpened(global::Android.Views.View drawerView)
		{
		}

		void IDrawerListener.OnDrawerSlide(global::Android.Views.View drawerView, float slideOffset)
		{
		}

		void IDrawerListener.OnDrawerStateChanged(int newState)
		{
			_presented = IsDrawerVisible(_masterLayout);
			UpdateIsPresented();
		}

		VisualElement IVisualElementRenderer.Element => Element;

		event EventHandler<VisualElementChangedEventArgs> IVisualElementRenderer.ElementChanged
		{
			add { ElementChanged += value; }
			remove { ElementChanged -= value; }
		}

		SizeRequest IVisualElementRenderer.GetDesiredSize(int widthConstraint, int heightConstraint)
		{
			Measure(widthConstraint, heightConstraint);
			return new SizeRequest(new Size(MeasuredWidth, MeasuredHeight));
		}

		void IVisualElementRenderer.SetElement(VisualElement element)
		{
			MasterDetailPage oldElement = Element;
			MasterDetailPage newElement = Element = element as MasterDetailPage;

			if (oldElement != null)
			{
				oldElement.BackButtonPressed -= OnBackButtonPressed;
				oldElement.PropertyChanged -= HandlePropertyChanged;
				oldElement.Appearing -= MasterDetailPageAppearing;
				oldElement.Disappearing -= MasterDetailPageDisappearing;
			}

			var statusBarHeight = 0;
			if (Forms.IsLollipopOrNewer)
				statusBarHeight = ((FormsAppCompatActivity)Context).GetStatusBarHeight();

			if (newElement != null)
			{
				if (_detailLayout == null)
				{
					_detailLayout = new MasterDetailContainer(newElement, false, Context)
					{
						TopPadding = statusBarHeight,
						LayoutParameters = new LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent)
					};

					_masterLayout = new MasterDetailContainer(newElement, true, Context)
					{
						LayoutParameters = new LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent) { Gravity = (int)GravityFlags.Start }
					};

					AddView(_detailLayout);
					AddView(_masterLayout);

					Device.Info.PropertyChanged += DeviceInfoPropertyChanged;

					AddDrawerListener(this);
				}

				UpdateBackgroundColor(newElement);
				UpdateBackgroundImage(newElement);

				UpdateMaster();
				UpdateDetail();

				newElement.BackButtonPressed += OnBackButtonPressed;
				newElement.PropertyChanged += HandlePropertyChanged;
				newElement.Appearing += MasterDetailPageAppearing;
				newElement.Disappearing += MasterDetailPageDisappearing;

				SetGestureState();

				Presented = newElement.IsPresented;

				newElement.SendViewInitialized(this);
			}

			OnElementChanged(oldElement, newElement);

			// Make sure to initialize this AFTER event is fired
			if (_tracker == null)
				_tracker = new VisualElementTracker(this);
		}

		VisualElementTracker IVisualElementRenderer.Tracker => _tracker;

		void IVisualElementRenderer.UpdateLayout()
		{
			_tracker?.UpdateLayout();
		}

		ViewGroup IVisualElementRenderer.ViewGroup => this;

		protected override void Dispose(bool disposing)
		{
			if (disposing && !_disposed)
			{
				_disposed = true;

				if (_tracker != null)
				{
					_tracker.Dispose();
					_tracker = null;
				}

				if (_detailLayout != null)
				{
					_detailLayout.Dispose();
					_detailLayout = null;
				}

				if (_masterLayout != null)
				{
					_masterLayout.Dispose();
					_masterLayout = null;
				}

				Device.Info.PropertyChanged -= DeviceInfoPropertyChanged;

				if (Element != null)
				{
					Element.BackButtonPressed -= OnBackButtonPressed;
					Element.PropertyChanged -= HandlePropertyChanged;
					Element.Appearing -= MasterDetailPageAppearing;
					Element.Disappearing -= MasterDetailPageDisappearing;
					Element.ClearValue(Android.Platform.RendererProperty);
					Element = null;
				}
			}

			base.Dispose(disposing);
		}

		protected override void OnAttachedToWindow()
		{
			base.OnAttachedToWindow();
			Element.SendAppearing();
		}

		protected override void OnDetachedFromWindow()
		{
			base.OnDetachedFromWindow();
			Element.SendDisappearing();
		}

		protected virtual void OnElementChanged(VisualElement oldElement, VisualElement newElement)
		{
			ElementChanged?.Invoke(this, new VisualElementChangedEventArgs(oldElement, newElement));
		}

		protected override void OnLayout(bool changed, int l, int t, int r, int b)
		{
			base.OnLayout(changed, l, t, r, b);
			//hack to make the split layout handle touches the full width
			if (Element.ShouldShowSplitMode && _masterLayout != null)
				_masterLayout.Right = r;
		}

		async void DeviceInfoPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (nameof(Device.Info.CurrentOrientation) == e.PropertyName)
			{
				if (!Element.ShouldShowSplitMode && Presented)
				{
					Element.CanChangeIsPresented = true;
					//hack : when the orientation changes and we try to close the Master on Android		
					//sometimes Android picks the width of the screen previous to the rotation 		
					//this leaves a little of the master visible, the hack is to delay for 50ms closing the drawer
					await Task.Delay(100);
					CloseDrawer(_masterLayout);
				}
				UpdateSplitViewLayout();
			}
		}

		event EventHandler<VisualElementChangedEventArgs> ElementChanged;

		void HandleMasterPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
		}

		void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "Master")
				UpdateMaster();
			else if (e.PropertyName == "Detail")
				UpdateDetail();
			else if (e.PropertyName == "IsGestureEnabled")
				SetGestureState();
			else if (e.PropertyName == MasterDetailPage.IsPresentedProperty.PropertyName)
			{
				_isPresentingFromCore = true;
				Presented = Element.IsPresented;
				_isPresentingFromCore = false;
			}
			else if (e.PropertyName == Page.BackgroundImageProperty.PropertyName)
				UpdateBackgroundImage(Element);
			else if (e.PropertyName == VisualElement.BackgroundColorProperty.PropertyName)
				UpdateBackgroundColor(Element);
		}

		void MasterDetailPageAppearing(object sender, EventArgs e)
		{
			Element.Master?.SendAppearing();
			Element.Detail?.SendAppearing();
		}

		void MasterDetailPageDisappearing(object sender, EventArgs e)
		{
			Element.Master?.SendDisappearing();
			Element.Detail?.SendDisappearing();
		}

		void OnBackButtonPressed(object sender, BackButtonPressedEventArgs backButtonPressedEventArgs)
		{
			if (!IsDrawerOpen((int)GravityFlags.Start) || _currentLockMode == LockModeLockedOpen)
				return;

			CloseDrawer((int)GravityFlags.Start);
			backButtonPressedEventArgs.Handled = true;
		}

		void SetGestureState()
		{
			SetDrawerLockMode(Element.IsGestureEnabled ? LockModeUnlocked : LockModeLockedClosed);
		}

		void SetLockMode(int lockMode)
		{
			if (_currentLockMode != lockMode)
			{
				SetDrawerLockMode(lockMode);
				_currentLockMode = lockMode;
			}
		}

		void UpdateBackgroundColor(Page view)
		{
			Color backgroundColor = view.BackgroundColor;
			if (backgroundColor.IsDefault)
				SetBackgroundColor(backgroundColor.ToAndroid());
		}

		void UpdateBackgroundImage(Page view)
		{
			string backgroundImage = view.BackgroundImage;
			if (!string.IsNullOrEmpty(backgroundImage))
				this.SetBackground(Context.Resources.GetDrawable(backgroundImage));
		}

		void UpdateDetail()
		{
			Context.HideKeyboard(this);
			_detailLayout.ChildView = Element.Detail;
		}

		void UpdateIsPresented()
		{
			if (_isPresentingFromCore)
				return;
			if (Presented != Element.IsPresented)
				((IElementController)Element).SetValueFromRenderer(MasterDetailPage.IsPresentedProperty, Presented);
		}

		void UpdateMaster()
		{
			MasterDetailContainer masterContainer = _masterLayout;
			if (masterContainer == null)
				return;

			if (masterContainer.ChildView != null)
				masterContainer.ChildView.PropertyChanged -= HandleMasterPropertyChanged;

			masterContainer.ChildView = Element.Master;
			if (Element.Master != null)
				Element.Master.PropertyChanged += HandleMasterPropertyChanged;
		}

		void UpdateSplitViewLayout()
		{
			if (Device.Idiom == TargetIdiom.Tablet)
			{
				bool isShowingSplit = Element.ShouldShowSplitMode || (Element.ShouldShowSplitMode && Element.MasterBehavior != MasterBehavior.Default && Element.IsPresented);
				SetLockMode(isShowingSplit ? LockModeLockedOpen : LockModeUnlocked);
				unchecked
				{
					SetScrimColor(isShowingSplit ? Color.Transparent.ToAndroid() : (int)DefaultScrimColor);
				}
			}
		}
	}
}