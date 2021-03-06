using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
#if __UNIFIED__
using Foundation;
using UIKit;

#else
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif

namespace Xamarin.Forms.Platform.iOS
{
	public class FormsApplicationDelegate : UIApplicationDelegate
	{
		Application _application;
		bool _isSuspended;
		UIWindow _window;

		protected FormsApplicationDelegate()
		{
		}

		public override bool ContinueUserActivity(UIApplication application, NSUserActivity userActivity, UIApplicationRestorationHandler completionHandler)
		{
			return true;
		}

		// now in background
		public override void DidEnterBackground(UIApplication uiApplication)
		{
			// applicationDidEnterBackground
		}

		// finish initialization before display to user
		public override bool FinishedLaunching(UIApplication uiApplication, NSDictionary launchOptions)
		{
			// check contents of launch options and evaluate why the app was launched and respond
			// initialize the important data structures
			// prepare you apps window and views for display
			// keep lightweight, anything long winded should be executed asynchronously on a secondary thread.
			// application:didFinishLaunchingWithOptions
			_window = new UIWindow(UIScreen.MainScreen.Bounds);

			if (_application == null)
				throw new InvalidOperationException("You MUST invoke LoadApplication () before calling base.FinishedLaunching ()");

			SetMainPage();
			_application.SendStart();
			return true;
		}

		// about to become foreground, last minute preparatuin
		public override void OnActivated(UIApplication uiApplication)
		{
			// applicationDidBecomeActive
			// execute any OpenGL ES drawing calls
			if (_application != null && _isSuspended)
			{
				_isSuspended = false;
				_application.SendResume();
			}
		}

		// transitioning to background
		public override async void OnResignActivation(UIApplication uiApplication)
		{
			// applicationWillResignActive
			if (_application != null)
			{
				_isSuspended = true;
				await _application.SendSleepAsync();
			}
		}

		public override void UserActivityUpdated(UIApplication application, NSUserActivity userActivity)
		{
		}

		// from background to foreground, not yet active
		public override void WillEnterForeground(UIApplication uiApplication)
		{
			// applicationWillEnterForeground
		}

		// TODO where to execute heavy code, storing state, sending to server, etc 

		// first chance to execute code at launch time
		public override bool WillFinishLaunching(UIApplication uiApplication, NSDictionary launchOptions)
		{
			// check contents of launch options and evaluate why the app was launched and respond
			// initialize the important data structures
			// prepare you apps window and views for display
			// keep lightweight, anything long winded should be executed asynchronously on a secondary thread.
			// application:willFinishLaunchingWithOptions
			// Restore ui state here
			return true;
		}

		// app is being terminated, not called if you app is suspended
		public override void WillTerminate(UIApplication uiApplication)
		{
			// applicationWillTerminate
			//application.SendTerminate ();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && _application != null)
				_application.PropertyChanged -= ApplicationOnPropertyChanged;

			base.Dispose(disposing);
		}

		protected void LoadApplication(Application application)
		{
			if (application == null)
				throw new ArgumentNullException("application");

			Application.Current = application;
			_application = application;

			application.PropertyChanged += ApplicationOnPropertyChanged;
		}

		void ApplicationOnPropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			if (args.PropertyName == "MainPage")
				UpdateMainPage();
		}

		void SetMainPage()
		{
			UpdateMainPage();
			_window.MakeKeyAndVisible();
		}

		void UpdateMainPage()
		{
			if (_application.MainPage == null)
				return;

			var platformRenderer = (PlatformRenderer)_window.RootViewController;
			_window.RootViewController = _application.MainPage.CreateViewController();
			if (platformRenderer != null)
				((IDisposable)platformRenderer.Platform).Dispose();
		}
	}
}