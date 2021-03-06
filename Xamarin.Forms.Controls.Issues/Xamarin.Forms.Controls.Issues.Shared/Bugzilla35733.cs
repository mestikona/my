﻿using System;

using Xamarin.Forms.CustomAttributes;
using System.Threading.Tasks;

#if UITEST
using Xamarin.UITest;
using NUnit.Framework;
#endif

namespace Xamarin.Forms.Controls
{
	[Preserve (AllMembers = true)]
	[Issue (IssueTracker.Bugzilla, 35733, "iOS WebView crashes when loading an URL with encoded parameters", PlatformAffected.iOS)]
	public class Bugzilla35733 : TestContentPage // or TestMasterDetailPage, etc ...
	{
		protected override void Init ()
		{
			var thisDoesNotWorkButton = new Button {
				Text = "This will crash",
				AutomationId = "btnGo"

			};
			thisDoesNotWorkButton.Clicked += async (object sender, EventArgs e) => await ShowLocation ("KÅRA");

			Content = new StackLayout {
				VerticalOptions = LayoutOptions.Center,
				Children = {
					thisDoesNotWorkButton
				}
			};
		}

		async Task ShowLocation(string locationString) 
		{
			var stringUri = string.Format("http://m.liu.se/karta/karta?l=en&px_location={0}", Uri.EscapeDataString(locationString));
			var uri = new Uri(stringUri);
			var webPage = new ContentPage {
				Title = "WebViewTest",
				Content = new WebView {
					Source = uri
				}
			};
			await Navigation.PushAsync(webPage);
		}

#if UITEST
		[Test]
		public void Bugzilla35733Test ()
		{
			RunningApp.WaitForElement (q => q.Marked ("btnGo"));
			RunningApp.Tap (q => q.Marked ("btnGo"));
			RunningApp.WaitForElement (q => q.Marked ("WebViewTest"));
			RunningApp.Screenshot ("I didn't crash");
		}
#endif
	}
}
