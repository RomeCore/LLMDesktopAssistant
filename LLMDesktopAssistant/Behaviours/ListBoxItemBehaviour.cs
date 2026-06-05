using System;
using System.Collections.Generic;
using System.Text;
using AngleSharp.Dom;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using LLMDesktopAssistant.UIExtensions.CodeBlockExtensions;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Behaviours
{
	public static class ListBoxItemBehaviour
	{
		public static readonly AttachedProperty<bool> IsParentListBoxItemEnabledProperty =
			AvaloniaProperty.RegisterAttached<Control, bool>(
				"IsParentListBoxItemEnabled",
				typeof(ListBoxItemBehaviour),
				true);

		public static void SetIsParentListBoxItemEnabled(Control element, bool value)
		{
			element.SetValue(IsParentListBoxItemEnabledProperty, value);
		}

		public static bool GetIsParentListBoxItemEnabled(Control element)
		{
			return element.GetValue(IsParentListBoxItemEnabledProperty);
		}

		static ListBoxItemBehaviour()
		{
			IsParentListBoxItemEnabledProperty.Changed.AddClassHandler<Control, bool>(
				(o, e) => IsExtendedChanged(o, e.GetNewValue<bool>()));
		}

		private static void IsExtendedChanged(Control element, bool isEnabled)
		{
			if (element.IsLoaded)
			{
				var parentListBoxItem = element.GetLogicalParent<ListBoxItem>();
				if (parentListBoxItem != null)
					parentListBoxItem.IsEnabled = isEnabled;
			}
			else
			{
				void Element_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
				{
					element.Loaded -= Element_Loaded;
					var parentListBoxItem = element.GetLogicalParent<ListBoxItem>();
					if (parentListBoxItem != null)
						parentListBoxItem.IsEnabled = isEnabled;
				}
				element.Loaded += Element_Loaded;
			}
		}
	}
}