using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Controls
{
	/// <summary>
	/// The control that displays markdown markup text.
	/// </summary>
	[ContentProperty(nameof(MarkdownText))]
	public partial class MarkdownControl : UserControl
	{
		public static readonly DependencyProperty MarkdownTextProperty =
			DependencyProperty.Register(nameof(MarkdownText), typeof(string), typeof(MarkdownControl), new PropertyMetadata(null, OnMarkdownTextChanged));

		private static void OnMarkdownTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is MarkdownControl markdownControl)
				markdownControl.OnMarkdownTextChanged(e.OldValue as string, e.NewValue as string);
		}

		/// <summary>
		/// Gets or sets the markdown text.
		/// </summary>
		public string MarkdownText
		{
			get => (string)GetValue(MarkdownTextProperty);
			set => SetValue(MarkdownTextProperty, value);
		}

		public MarkdownControl()
		{
			InitializeComponent();
		}

		private void OnMarkdownTextChanged(string? oldValue, string? newValue)
		{
			oldValue ??= string.Empty;
			newValue ??= string.Empty;

			MarkdownContent.Document = MarkdownParser.ParseDocument(newValue);
		}
	}
}