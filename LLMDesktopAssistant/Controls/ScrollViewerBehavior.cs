using System.Windows;
using System.Windows.Controls;

namespace LLMDesktopAssistant.Core.Controls
{
	public class ScrollViewerBehavior
	{
		public static bool GetAutoScrollEnabled(DependencyObject obj)
		{
			return (bool)obj.GetValue(AutoScrollEnabledProperty);
		}

		public static void SetAutoScrollEnabled(DependencyObject obj, bool value)
		{
			obj.SetValue(AutoScrollEnabledProperty, value);
		}

		private static readonly DependencyProperty IsAutoScrollingProperty =
			DependencyProperty.RegisterAttached("IsAutoScrolling", typeof(bool),
				typeof(ScrollViewerBehavior));

		public static readonly DependencyProperty AutoScrollEnabledProperty =
			DependencyProperty.RegisterAttached("AutoScrollEnabled", typeof(bool),
				typeof(ScrollViewerBehavior), new PropertyMetadata(false, OnAutoScrollEnabledChanged));

		private static void OnAutoScrollEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is ScrollViewer element)
			{
				if ((bool)e.OldValue == true)
					element.ScrollChanged -= Element_ScrollChanged;

				if ((bool)e.NewValue == true)
					element.ScrollChanged += Element_ScrollChanged;
			}
		}

		private static void Element_ScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			var scrollViewer = sender as ScrollViewer;

			if (e.ExtentHeightChange == 0)
				scrollViewer.SetValue(IsAutoScrollingProperty, scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight);

			if ((bool)scrollViewer.GetValue(IsAutoScrollingProperty) && e.ExtentHeightChange != 0)
				scrollViewer.ScrollToVerticalOffset(scrollViewer.ExtentHeight);
		}
	}
}