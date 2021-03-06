﻿using System;

using Xamarin.Forms.CustomAttributes;

namespace Xamarin.Forms.Controls
{
	[Preserve(AllMembers = true)]
	[Issue(IssueTracker.Bugzilla, 39853, "BorderRadius ignored on UWP", PlatformAffected.WinRT)]
	public class Bugzilla39853 : TestContentPage
	{
		public class RoundedButton : Xamarin.Forms.Button
		{
			public RoundedButton(int radius)
			{
				base.BorderRadius = radius;
				base.WidthRequest = 2 * radius;
				base.HeightRequest = 2 * radius;
				HorizontalOptions = LayoutOptions.Center;
				VerticalOptions = LayoutOptions.Center;
				BackgroundColor = Color.Aqua;
				BorderColor = Color.White;
				TextColor = Color.Purple;
				Text = "YAY";
				Image = new FileImageSource { File = "crimson.jpg" };
			}

			public new int BorderRadius
			{
				get
				{
					return base.BorderRadius;
				}

				set
				{
					base.WidthRequest = 2 * value;
					base.HeightRequest = 2 * value;
					base.BorderRadius = value;
				}
			}

			public new double WidthRequest
			{
				get
				{
					return base.WidthRequest;
				}

				set
				{
					base.WidthRequest = value;
					base.HeightRequest = value;
					base.BorderRadius = ((int)value) / 2;
				}
			}

			public new double HeightRequest
			{
				get
				{
					return base.HeightRequest;
				}

				set
				{
					base.WidthRequest = value;
					base.HeightRequest = value;
					base.BorderRadius = ((int)value) / 2;
				}
			}

		}
		protected override void Init()
		{
			Content = new RoundedButton(100);
		}
	}
}
