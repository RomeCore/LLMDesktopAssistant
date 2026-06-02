using Avalonia;
using Avalonia.Controls;

namespace LLMDesktopAssistant.Behaviours
{
	public class ScrollViewerBehavior
	{
		public static bool GetAutoScrollEnabled(AvaloniaObject obj)
		{
			return (bool)obj.GetValue(AutoScrollEnabledProperty);
		}

		public static void SetAutoScrollEnabled(AvaloniaObject obj, bool value)
		{
			obj.SetValue(AutoScrollEnabledProperty, value);
		}

		private static readonly AttachedProperty<bool> IsAutoScrollingVerticalProperty =
			AvaloniaProperty.RegisterAttached<ScrollViewer, bool>("IsAutoScrollingVertical",
				typeof(ScrollViewerBehavior));

		public static readonly AttachedProperty<bool> AutoScrollEnabledProperty =
			AvaloniaProperty.RegisterAttached<ScrollViewer, bool>("AutoScrollEnabled",
				typeof(ScrollViewerBehavior), false);

		static ScrollViewerBehavior()
		{
			AutoScrollEnabledProperty.Changed.AddClassHandler<ScrollViewer>(OnAutoScrollEnabledChanged);
		}

		private static void OnAutoScrollEnabledChanged(ScrollViewer element, AvaloniaPropertyChangedEventArgs e)
		{
			if ((bool)e.OldValue! == true)
				element.ScrollChanged -= Element_ScrollChanged;

			if ((bool)e.NewValue! == true)
				element.ScrollChanged += Element_ScrollChanged;
		}

		private static void Element_ScrollChanged(object? sender, ScrollChangedEventArgs e)
		{
			var scrollViewer = sender as ScrollViewer;
			if (scrollViewer == null) return;

			if (scrollViewer.Offset.Y >= scrollViewer.ScrollBarMaximum.Y - 1)
			{
				scrollViewer.SetValue(IsAutoScrollingVerticalProperty, scrollViewer.Offset.Y == scrollViewer.ScrollBarMaximum.Y);
				if (scrollViewer.GetValue(IsAutoScrollingVerticalProperty))
					scrollViewer.SetCurrentValue(ScrollViewer.OffsetProperty, new Vector(scrollViewer.Offset.X, double.PositiveInfinity));
			}
		}
	}
}