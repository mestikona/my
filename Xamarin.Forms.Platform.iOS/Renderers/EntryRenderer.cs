using System;
using System.Drawing;
using System.Runtime.Remoting.Channels;
using System.ComponentModel;
#if __UNIFIED__
using UIKit;

#else
using MonoTouch.UIKit;
#endif

namespace Xamarin.Forms.Platform.iOS
{
	public class EntryRenderer : ViewRenderer<Entry, UITextField>
	{
		UIColor _defaultTextColor;

		public EntryRenderer()
		{
			Frame = new RectangleF(0, 20, 320, 40);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (Control != null)
				{
					Control.EditingDidBegin -= OnEditingBegan;
					Control.EditingChanged -= OnEditingChanged;
					Control.EditingDidEnd -= OnEditingEnded;
				}
			}

			base.Dispose(disposing);
		}

		protected override void OnElementChanged(ElementChangedEventArgs<Entry> e)
		{
			base.OnElementChanged(e);

			var textField = Control;

			if (Control == null)
			{
				SetNativeControl(textField = new UITextField(RectangleF.Empty));

				_defaultTextColor = textField.TextColor;
				textField.BorderStyle = UITextBorderStyle.RoundedRect;

				textField.EditingChanged += OnEditingChanged;

				textField.ShouldReturn = OnShouldReturn;

				textField.EditingDidBegin += OnEditingBegan;
				textField.EditingDidEnd += OnEditingEnded;
			}

			if (e.NewElement != null)
			{
				UpdatePlaceholder();
				UpdatePassword();
				UpdateText();
				UpdateColor();
				UpdateFont();
				UpdateKeyboard();
				UpdateAlignment();
			}
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == Entry.PlaceholderProperty.PropertyName || e.PropertyName == Entry.PlaceholderColorProperty.PropertyName)
				UpdatePlaceholder();
			else if (e.PropertyName == Entry.IsPasswordProperty.PropertyName)
				UpdatePassword();
			else if (e.PropertyName == Entry.TextProperty.PropertyName)
				UpdateText();
			else if (e.PropertyName == Entry.TextColorProperty.PropertyName)
				UpdateColor();
			else if (e.PropertyName == Xamarin.Forms.InputView.KeyboardProperty.PropertyName)
				UpdateKeyboard();
			else if (e.PropertyName == Entry.HorizontalTextAlignmentProperty.PropertyName)
				UpdateAlignment();
			else if (e.PropertyName == Entry.FontAttributesProperty.PropertyName)
				UpdateFont();
			else if (e.PropertyName == Entry.FontFamilyProperty.PropertyName)
				UpdateFont();
			else if (e.PropertyName == Entry.FontSizeProperty.PropertyName)
				UpdateFont();
			else if (e.PropertyName == VisualElement.IsEnabledProperty.PropertyName)
			{
				UpdateColor();
				UpdatePlaceholder();
			}

			base.OnElementPropertyChanged(sender, e);
		}

		void OnEditingBegan(object sender, EventArgs e)
		{
			((IElementController)Element).SetValueFromRenderer(VisualElement.IsFocusedPropertyKey, true);
		}

		void OnEditingChanged(object sender, EventArgs eventArgs)
		{
			((IElementController)Element).SetValueFromRenderer(Entry.TextProperty, Control.Text);
		}

		void OnEditingEnded(object sender, EventArgs e)
		{
			((IElementController)Element).SetValueFromRenderer(VisualElement.IsFocusedPropertyKey, false);
		}

		bool OnShouldReturn(UITextField view)
		{
			Control.ResignFirstResponder();
			Element.SendCompleted();
			return true;
		}

		void UpdateAlignment()
		{
			Control.TextAlignment = Element.HorizontalTextAlignment.ToNativeTextAlignment();
		}

		void UpdateColor()
		{
			var textColor = Element.TextColor;

			if (textColor.IsDefault || !Element.IsEnabled)
				Control.TextColor = _defaultTextColor;
			else
				Control.TextColor = textColor.ToUIColor();
		}

		void UpdateFont()
		{
			Control.Font = Element.ToUIFont();
		}

		void UpdateKeyboard()
		{
			Control.ApplyKeyboard(Element.Keyboard);
		}

		void UpdatePassword()
		{
			if (Element.IsPassword && Control.IsFirstResponder)
			{
				Control.Enabled = false;
				Control.SecureTextEntry = true;
				Control.Enabled = Element.IsEnabled;
				Control.BecomeFirstResponder();
			}
			else
				Control.SecureTextEntry = Element.IsPassword;
		}

		void UpdatePlaceholder()
		{
			var formatted = (FormattedString)Element.Placeholder;

			if (formatted == null)
				return;

			var targetColor = Element.PlaceholderColor;

			// Placeholder default color is 70% gray
			// https://developer.apple.com/library/prerelease/ios/documentation/UIKit/Reference/UITextField_Class/index.html#//apple_ref/occ/instp/UITextField/placeholder

			var color = Element.IsEnabled && !targetColor.IsDefault ? targetColor : ColorExtensions.SeventyPercentGrey.ToColor();

			Control.AttributedPlaceholder = formatted.ToAttributed(Element, color);
		}

		void UpdateText()
		{
			// ReSharper disable once RedundantCheckBeforeAssignment
			if (Control.Text != Element.Text)
				Control.Text = Element.Text;
		}
	}
}