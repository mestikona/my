﻿using System.Collections.Specialized;
using System.ComponentModel;
using Android.Content;
using Android.Support.V4.View;
using Android.Views;

namespace Xamarin.Forms.Platform.Android.AppCompat
{
	public class CarouselPageRenderer : VisualElementRenderer<CarouselPage>, ViewPager.IOnPageChangeListener
	{
		bool _disposed;
		FormsViewPager _viewPager;

		public CarouselPageRenderer()
		{
			AutoPackage = false;
		}

		void ViewPager.IOnPageChangeListener.OnPageScrolled(int position, float positionOffset, int positionOffsetPixels)
		{
		}

		void ViewPager.IOnPageChangeListener.OnPageScrollStateChanged(int state)
		{
		}

		void ViewPager.IOnPageChangeListener.OnPageSelected(int position)
		{
			Element.CurrentPage = Element.Children[position];
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && !_disposed)
			{
				_disposed = true;
				RemoveAllViews();
				foreach (ContentPage pageToRemove in Element.Children)
				{
					IVisualElementRenderer pageRenderer = Android.Platform.GetRenderer(pageToRemove);
					if (pageRenderer != null)
					{
						pageRenderer.ViewGroup.RemoveFromParent();
						pageRenderer.Dispose();
					}
					pageToRemove.ClearValue(Android.Platform.RendererProperty);
				}

				if (_viewPager != null)
				{
					_viewPager.Adapter.Dispose();
					_viewPager.Dispose();
					_viewPager = null;
				}

				if (Element != null)
					Element.InternalChildren.CollectionChanged -= OnChildrenCollectionChanged;
			}

			base.Dispose(disposing);
		}

		protected override void OnAttachedToWindow()
		{
			base.OnAttachedToWindow();
			Element.SendAppearing();
		}

		protected override void OnDetachedFromWindow()
		{
			base.OnDetachedFromWindow();
			Element.SendDisappearing();
		}

		protected override void OnElementChanged(ElementChangedEventArgs<CarouselPage> e)
		{
			base.OnElementChanged(e);

			var activity = (FormsAppCompatActivity)Context;

			if (e.OldElement != null)
				e.OldElement.InternalChildren.CollectionChanged -= OnChildrenCollectionChanged;

			if (e.NewElement != null)
			{
				FormsViewPager pager =
					_viewPager =
					new FormsViewPager(activity)
					{
						OverScrollMode = OverScrollMode.Never,
						EnableGesture = true,
						LayoutParameters = new LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent),
						Adapter = new FormsFragmentPagerAdapter<ContentPage>(e.NewElement, activity.SupportFragmentManager) { CountOverride = e.NewElement.Children.Count }
					};
				pager.Id = FormsAppCompatActivity.GetUniqueId();
				pager.AddOnPageChangeListener(this);

				AddView(pager);
				CarouselPage carouselPage = e.NewElement;
				if (carouselPage.CurrentPage != null)
					ScrollToCurrentPage();

				UpdateIgnoreContainerAreas();
				carouselPage.InternalChildren.CollectionChanged += OnChildrenCollectionChanged;
			}
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == "CurrentPage")
				ScrollToCurrentPage();
		}

		protected override void OnLayout(bool changed, int l, int t, int r, int b)
		{
			FormsViewPager pager = _viewPager;
			Context context = Context;
			int width = r - l;
			int height = b - t;

			pager.Measure(MeasureSpecFactory.MakeMeasureSpec(width, MeasureSpecMode.AtMost), MeasureSpecFactory.MakeMeasureSpec(height, MeasureSpecMode.AtMost));

			if (width > 0 && height > 0)
			{
				Element.ContainerArea = new Rectangle(0, 0, context.FromPixels(width), context.FromPixels(height));
				pager.Layout(0, 0, width, b);
			}

			base.OnLayout(changed, l, t, r, b);
		}

		void OnChildrenCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			FormsViewPager pager = _viewPager;

			((FormsFragmentPagerAdapter<ContentPage>)pager.Adapter).CountOverride = Element.Children.Count;
			pager.Adapter.NotifyDataSetChanged();

			UpdateIgnoreContainerAreas();
		}

		void ScrollToCurrentPage()
		{
			_viewPager.SetCurrentItem(Element.Children.IndexOf(Element.CurrentPage), true);
		}

		void UpdateIgnoreContainerAreas()
		{
			foreach (ContentPage child in Element.Children)
				child.IgnoresContainerArea = child is NavigationPage;
		}
	}
}