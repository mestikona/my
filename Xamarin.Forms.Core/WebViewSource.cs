using System;

namespace Xamarin.Forms
{
	public abstract class WebViewSource : BindableObject
	{
		public static implicit operator WebViewSource(Uri url)
		{
			return new UrlWebViewSource { Url = url?.AbsoluteUri };
		}

		public static implicit operator WebViewSource(string url)
		{
			return new UrlWebViewSource { Url = url };
		}

		protected void OnSourceChanged()
		{
			EventHandler eh = SourceChanged;
			if (eh != null)
				eh(this, EventArgs.Empty);
		}

		internal abstract void Load(IWebViewRenderer renderer);

		internal event EventHandler SourceChanged;
	}
}