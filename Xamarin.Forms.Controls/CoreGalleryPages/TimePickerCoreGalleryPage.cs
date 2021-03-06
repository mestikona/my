using System;

using Xamarin.Forms.CustomAttributes;

namespace Xamarin.Forms.Controls
{
	internal class TimePickerCoreGalleryPage : CoreGalleryPage<TimePicker>
	{
		protected override bool SupportsTapGestureRecognizer
		{
			get { return false; }
		}

		protected override void Build (StackLayout stackLayout)
		{
			base.Build (stackLayout);
			var formatContainer = new ViewContainer<TimePicker> (Test.TimePicker.Format, new TimePicker { Format = "HH-mm-ss" });
			var timeContainer = new ViewContainer<TimePicker> (Test.TimePicker.Time, new TimePicker { Time = new TimeSpan (14, 45, 50) });

			Add (formatContainer);
			Add (timeContainer);
		}
	}
}