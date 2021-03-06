using System;
using System.ComponentModel;
using Android.Opengl;
using Javax.Microedition.Khronos.Opengles;
using EGLConfig = Javax.Microedition.Khronos.Egl.EGLConfig;
using Object = Java.Lang.Object;

namespace Xamarin.Forms.Platform.Android
{
	internal class OpenGLViewRenderer : ViewRenderer<OpenGLView, GLSurfaceView>
	{
		bool _disposed;

		public OpenGLViewRenderer()
		{
			AutoPackage = false;
		}

		protected override void Dispose(bool disposing)
		{
			if (!_disposed && disposing)
			{
				_disposed = true;

				if (Element != null)
					((IOpenGlViewController)Element).DisplayRequested -= Display;
			}
			base.Dispose(disposing);
		}

		protected override void OnElementChanged(ElementChangedEventArgs<OpenGLView> e)
		{
			base.OnElementChanged(e);

			if (e.OldElement != null)
				((IOpenGlViewController)Element).DisplayRequested -= Display;

			if (e.NewElement != null)
			{
				GLSurfaceView surfaceView = Control;
				if (surfaceView == null)
				{
					surfaceView = new GLSurfaceView(Context);
					surfaceView.SetEGLContextClientVersion(2);
					SetNativeControl(surfaceView);
				}

				((IOpenGlViewController)Element).DisplayRequested += Display;
				surfaceView.SetRenderer(new Renderer(Element));
				SetRenderMode();
			}
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == OpenGLView.HasRenderLoopProperty.PropertyName)
				SetRenderMode();
		}

		void Display(object sender, EventArgs eventArgs)
		{
			if (Element.HasRenderLoop)
				return;
			Control.RequestRender();
		}

		void SetRenderMode()
		{
			Control.RenderMode = Element.HasRenderLoop ? Rendermode.Continuously : Rendermode.WhenDirty;
		}

		class Renderer : Object, GLSurfaceView.IRenderer
		{
			readonly OpenGLView _model;
			Rectangle _rect;

			public Renderer(OpenGLView model)
			{
				_model = model;
			}

			public void OnDrawFrame(IGL10 gl)
			{
				Action<Rectangle> onDisplay = _model.OnDisplay;
				if (onDisplay == null)
					return;
				onDisplay(_rect);
			}

			public void OnSurfaceChanged(IGL10 gl, int width, int height)
			{
				_rect = new Rectangle(0.0, 0.0, width, height);
			}

			public void OnSurfaceCreated(IGL10 gl, EGLConfig config)
			{
			}
		}
	}
}