using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace LLMDesktopAssistant.Core.Controls
{
	[ContentProperty(nameof(Content))]
	public class SwitchItem : ContentControl
	{
		public static readonly DependencyProperty CaseProperty =
			DependencyProperty.Register(nameof(Case), typeof(object), typeof(SwitchItem),
				new PropertyMetadata(null));

		public static readonly DependencyProperty IsDefaultProperty =
			DependencyProperty.Register(nameof(IsDefault), typeof(bool), typeof(SwitchItem),
				new PropertyMetadata(false));

		public static readonly DependencyProperty IsActiveProperty =
			DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(SwitchItem),
				new PropertyMetadata(false, OnIsActiveChanged));

		public object Case
		{
			get => GetValue(CaseProperty);
			set => SetValue(CaseProperty, value);
		}

		public bool IsDefault
		{
			get => (bool)GetValue(IsDefaultProperty);
			set => SetValue(IsDefaultProperty, value);
		}

		public bool IsActive
		{
			get => (bool)GetValue(IsActiveProperty);
			set => SetValue(IsActiveProperty, value);
		}

		private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var item = (SwitchItem)d;
			item.Visibility = item.IsActive ? Visibility.Visible : Visibility.Collapsed;
		}
	}
}