﻿using System;
using Xamarin.Forms.CustomAttributes;

namespace Xamarin.Forms.Controls
{
	[Preserve (AllMembers=true)]
	[Issue (IssueTracker.Github, 2597, "Stepper control .IsEnabled doesn't work", PlatformAffected.Android)]
	public class Issue2597 : ContentPage
	{
		Label _label;

		public Issue2597()
		{
			Label header = new Label
			{
				Text = "Stepper",
				HorizontalOptions = LayoutOptions.Center
			};

			Stepper stepper = new Stepper
			{
				Minimum = 0,
				Maximum = 10,
				Increment = 0.1,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.CenterAndExpand,
				IsEnabled = false
			};
			stepper.ValueChanged += OnStepperValueChanged;

			_label = new Label
			{
				Text = "Stepper value is 0",
				Font = Font.SystemFontOfSize(NamedSize.Large),
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.CenterAndExpand
			};

			// Accomodate iPhone status bar.
			Padding = new Thickness(10, Device.OnPlatform(20, 0, 0), 10,
				5);

			// Build the page.
			Content = new StackLayout
			{
				Children =
				{
					header,
					stepper,
					_label
				}
				};
		}

		void OnStepperValueChanged(object sender, ValueChangedEventArgs e)
		{
			_label.Text = string.Format("Stepper value is {0:F1}", e.NewValue);
		}
	}
}

