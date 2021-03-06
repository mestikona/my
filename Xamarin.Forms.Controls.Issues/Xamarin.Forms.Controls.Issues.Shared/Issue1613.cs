﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms.CustomAttributes;
using Xamarin.Forms.Maps;

namespace Xamarin.Forms.Controls.TestCasesPages
{
	[Preserve (AllMembers=true)]
	[Issue (IssueTracker.Github, 1613, "Map.GetSizeRequest always returns map's current size", PlatformAffected.Android | PlatformAffected.iOS)]
	public class Issue1613 : ContentPage
	{
		public Issue1613 ()
		{
			Build ();
		}

		async void Build ()
		{
			var image = new Image {
				Source = "http://www.califliving.com/title24-energy/images/sanfrancisco.jpg",
				Aspect = Aspect.AspectFill,
				Opacity = 0.5,
			};

			var name = new Label {
				Text = "Foo",
				XAlign = TextAlignment.Center,
				YAlign = TextAlignment.Center,
				Font = Font.SystemFontOfSize(30, FontAttributes.Bold),
				TextColor = Color.White,
			};

			var nameView = new AbsoluteLayout {
				HeightRequest = 170,
				BackgroundColor = Color.Black,
				Children = { 
					{image, new Rectangle(0, 0, 1, 1), AbsoluteLayoutFlags.All},  
					{name, new Rectangle(0, 0, 1, 1), AbsoluteLayoutFlags.All} 
				},
			};
				
			var addressLabel = new Label {
				Text = "Loading address…",
				XAlign = TextAlignment.Center,
				YAlign = TextAlignment.Center,
			};
										
			var map = new Map {
				VerticalOptions = LayoutOptions.FillAndExpand,
			};

			Content = new StackLayout {
				Children = { nameView, addressLabel, map },
			};

			await Task.Delay (1000);
			addressLabel.Text = "Updated with new\nmultiline\nlabel";
		}
	}
}
