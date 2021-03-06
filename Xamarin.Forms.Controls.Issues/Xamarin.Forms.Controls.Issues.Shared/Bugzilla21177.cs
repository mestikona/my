﻿using System;

using Xamarin.Forms.CustomAttributes;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

#if UITEST
using Xamarin.UITest.iOS;
using Xamarin.UITest;
using NUnit.Framework;
#endif

namespace Xamarin.Forms.Controls
{
	[Preserve(AllMembers = true)]
	[Issue(IssueTracker.Bugzilla, 21177, "Using a UICollectionView in a ViewRenderer results in issues with selection")]
	public class Bugzilla21177 : TestContentPage
	{
		public class CollectionView : View
		{
			public event EventHandler<int> ItemSelected;

			public void InvokeItemSelected(int index)
			{
				if (ItemSelected != null)
				{
					ItemSelected.Invoke(this, index);
				}
			}
		}

		protected override void Init()
		{
			var view = new CollectionView() { AutomationId = "view" };
			view.ItemSelected += View_ItemSelected;
			Content = view;
		}

		private void View_ItemSelected(object sender, int e)
		{
			DisplayAlert("Success", "Success", "Cancel");
		}

#if UITEST
		[Test]
		public void Bugzilla21177Test()
		{
			if (RunningApp is iOSApp)
			{
				RunningApp.WaitForElement(q => q.Marked("#1"));
				RunningApp.Tap(q => q.Marked("#1"));
				RunningApp.WaitForElement(q => q.Marked("Success"));
			}
		}
#endif
	}
}
