using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LLMDesktopAssistant.Core.Controls
{
	public static class FlowDocumentScrollViewerBehavior
	{
		public static readonly DependencyProperty PassThroughMouseWheelProperty =
			DependencyProperty.RegisterAttached(
				"PassThroughMouseWheel",
				typeof(bool),
				typeof(FlowDocumentScrollViewerBehavior),
				new PropertyMetadata(false, OnPassThroughMouseWheelChanged));

		public static void SetPassThroughMouseWheel(FlowDocumentScrollViewer element, bool value)
			=> element.SetValue(PassThroughMouseWheelProperty, value);

		public static bool GetPassThroughMouseWheel(FlowDocumentScrollViewer element)
			=> (bool)element.GetValue(PassThroughMouseWheelProperty);

		private static void OnPassThroughMouseWheelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is FlowDocumentScrollViewer viewer && (bool)e.NewValue)
			{
				viewer.PreviewMouseWheel += (sender, args) =>
				{
					args.Handled = true;

					var parent = VisualTreeHelper.GetParent(viewer);
					while (parent != null)
					{
						if (parent is UIElement uiElement)
						{
							var newArgs = new MouseWheelEventArgs(args.MouseDevice, args.Timestamp, args.Delta)
							{
								RoutedEvent = UIElement.MouseWheelEvent,
							};
							uiElement.RaiseEvent(newArgs);
							break;
						}
						parent = VisualTreeHelper.GetParent(parent);
					}
				};

				viewer.PreviewMouseLeftButtonDown += (sender, args) =>
				{
					if (viewer.IsFocused || viewer.IsKeyboardFocusWithin)
					{
						Keyboard.ClearFocus();
					}
				};
			}
		}
	}
}