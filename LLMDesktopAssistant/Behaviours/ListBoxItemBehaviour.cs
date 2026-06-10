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

		public static readonly AttachedProperty<bool> IsParentListBoxItemVisibleProperty =
			AvaloniaProperty.RegisterAttached<Control, bool>(
				"IsParentListBoxItemVisible",
				typeof(ListBoxItemBehaviour),
				true);

		public static void SetIsParentListBoxItemVisible(Control element, bool value)
		{
			element.SetValue(IsParentListBoxItemVisibleProperty, value);
		}

		public static bool GetIsParentListBoxItemVisible(Control element)
		{
			return element.GetValue(IsParentListBoxItemVisibleProperty);
		}

		public static readonly AttachedProperty<Dock> ParentListBoxItemDockProperty =
			AvaloniaProperty.RegisterAttached<Control, Dock>(
				"ParentListBoxItemDock",
				typeof(ListBoxItemBehaviour),
				Dock.Top);

		public static void SetParentListBoxItemDock(Control element, Dock value)
		{
			element.SetValue(ParentListBoxItemDockProperty, value);
		}

		public static Dock GetParentListBoxItemDock(Control element)
		{
			return element.GetValue(ParentListBoxItemDockProperty);
		}

		static ListBoxItemBehaviour()
		{
			IsParentListBoxItemEnabledProperty.Changed.AddClassHandler<Control, bool>(
				(o, e) =>
				{
					ParentListBoxItemPropertyChanged(o, (l, v) => l.IsEnabled = v, e.GetNewValue<bool>());
				});
			IsParentListBoxItemVisibleProperty.Changed.AddClassHandler<Control, bool>(
				(o, e) =>
				{
					ParentListBoxItemPropertyChanged(o, (l, v) => l.IsVisible = v, e.GetNewValue<bool>());
				});
			ParentListBoxItemDockProperty.Changed.AddClassHandler<Control, Dock>(
				(o, e) =>
				{
					ParentListBoxItemPropertyChanged(o, (l, v) => DockPanel.SetDock(l, v), e.GetNewValue<Dock>());
				});
		}

		private static void ParentListBoxItemPropertyChanged<T>(Control element, Action<ListBoxItem, T> setter, T newValue)
		{
			if (element.IsLoaded)
			{
				var parentListBoxItem = element.GetLogicalParent<ListBoxItem>();
				if (parentListBoxItem != null)
					setter(parentListBoxItem, newValue);
			}
			else
			{
				void Element_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
				{
					element.Loaded -= Element_Loaded;
					var parentListBoxItem = element.GetLogicalParent<ListBoxItem>();
					if (parentListBoxItem != null)
						setter(parentListBoxItem, newValue);
				}
				element.Loaded += Element_Loaded;
			}
		}
	}
}