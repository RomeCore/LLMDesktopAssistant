using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Serilog;

namespace LLMDesktopAssistant.Controls.Behaviours
{
	public static class DialogLayoutBehaviour
	{
		public static readonly AttachedProperty<Thickness?> DialogMarginProperty =
			AvaloniaProperty.RegisterAttached<Layoutable, Thickness?>(
				"DialogMargin",
				typeof(DialogLayoutBehaviour),
				null);

		public static void SetDialogMargin(Layoutable element, Thickness? value)
		{
			element.SetValue(DialogMarginProperty, value);
		}

		public static Thickness? GetDialogMargin(Layoutable element)
		{
			return element.GetValue(DialogMarginProperty);
		}

		static DialogLayoutBehaviour()
		{
			DialogMarginProperty.Changed.AddClassHandler<Layoutable>(OnDialogMarginChanged);
		}

		private static void OnDialogMarginChanged(Layoutable element, AvaloniaPropertyChangedEventArgs e)
		{
			OnAttachedToVisualTree(element, () =>
			{
				var dialogBorder = FindDialogBorder(element);
				if (dialogBorder == null)
				{
					Log.Warning("Could not find dialog background for element: {Element}", element);
					return;
				}

				if (e.NewValue is Thickness margin)
				{
					dialogBorder.HorizontalAlignment = HorizontalAlignment.Stretch;
					dialogBorder.VerticalAlignment = VerticalAlignment.Stretch;
					dialogBorder.Margin = margin;
				}
				else
				{
					dialogBorder.HorizontalAlignment = HorizontalAlignment.Center;
					dialogBorder.VerticalAlignment = VerticalAlignment.Center;
					dialogBorder.Margin = new Thickness(0);
				}
			});
		}

		private static void OnAttachedToVisualTree(Layoutable element, Action action)
		{
			if (element.IsAttachedToVisualTree())
			{
				action();
				return;
			}

			void Element_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
			{
				action();
				element.AttachedToVisualTree -= Element_AttachedToVisualTree;
			}

			element.AttachedToVisualTree += Element_AttachedToVisualTree;
		}

		private static Border? FindDialogBorder(Layoutable element)
		{
			return element.FindAncestorOfType<Border>(false, b => b.Name == "PART_Dialog_ContentBackground");

			/* Авалония говно
			StyledElement? current = element.Parent;

			while (current != null)
			{
				if (current.Name == "PART_Dialog_ContentBackground" && current is Border border)
					return border;

				current = current.Parent;
			}

			return null;*/
		}
	}
}
