using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using MoonSharp.Interpreter.Debugging;

namespace LLMDesktopAssistant.Core.Controls
{
	[ContentProperty(nameof(Items))]
	public class Switch : Control, IAddChild
	{
		public static readonly DependencyProperty ValueProperty =
			DependencyProperty.Register(nameof(Value), typeof(object), typeof(Switch),
				new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnValueChanged));

		private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var switchControl = (Switch)d;
			switchControl.UpdateContent();
		}

		/// <summary>
		/// Gets or sets the current value of the switch.
		/// </summary>
		public object Value
		{
			get => GetValue(ValueProperty);
			set => SetValue(ValueProperty, value);
		}

		/// <summary>
		/// Gets the collection of switch items.
		/// </summary>
		public SwitchItemCollection Items { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="Switch"/> class.
		/// </summary>
		public Switch()
		{
			Items = [];

			Items.CollectionChanged += OnItemsChanged;
		}

		private void OnItemsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			UpdateContent();
		}

		private void UpdateContent()
		{
			if (Items.Count == 0)
				return;

			SwitchItem? selectedItem = null;
			var value = Value;

			foreach (var item in Items)
			{
				if (item == null)
					continue;

				if (Equals(item.Case, value))
				{
					selectedItem = item;
					break;
				}

				if (item.IsDefault && selectedItem == null)
				{
					selectedItem = item;
				}
			}

			foreach (var item in Items)
			{
				if (item != null)
				{
					item.IsActive = (item == selectedItem);
				}
			}
		}

		void IAddChild.AddChild(object value)
		{
			if (value is SwitchItem item)
			{
				Items.Add(item);
			}
		}

		void IAddChild.AddText(string text)
		{
		}
	}
}