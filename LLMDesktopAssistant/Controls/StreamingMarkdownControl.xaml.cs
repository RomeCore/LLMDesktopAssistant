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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LLMDesktopAssistant.Core.Utils;

namespace LLMDesktopAssistant.Core.Controls
{
	public partial class StreamingMarkdownControl : UserControl
	{
		public static readonly DependencyProperty MarkdownTextProperty =
			DependencyProperty.Register("MarkdownText", typeof(string), typeof(StreamingMarkdownControl), new PropertyMetadata(null, OnMarkdownTextChanged));

		public static readonly DependencyProperty CompletedProperty =
			DependencyProperty.Register("Completed", typeof(bool), typeof(StreamingMarkdownControl), new PropertyMetadata(false, OnCompletedChanged));

		private static void OnMarkdownTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is StreamingMarkdownControl markdownControl)
				markdownControl.OnMarkdownTextChanged(e.OldValue as string, e.NewValue as string);
		}

		private static void OnCompletedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is StreamingMarkdownControl markdownControl)
				markdownControl.OnCompletedChanged((bool)e.OldValue, (bool)e.NewValue);
		}

		/// <summary>
		/// Gets or sets the markdown text.
		/// </summary>
		public string MarkdownText
		{
			get => (string)GetValue(MarkdownTextProperty);
			set => SetValue(MarkdownTextProperty, value);
		}

		/// <summary>
		/// Gets or sets whether the streaming markdown text has been completed.
		/// </summary>
		public bool Completed
		{
			get => (bool)GetValue(CompletedProperty);
			set => SetValue(CompletedProperty, value);
		}

		public StreamingMarkdownControl()
		{
			InitializeComponent();
		}

		private int _accumulatedMarkdownLen = 0;
		private void OnMarkdownTextChanged(string? oldValue, string? newValue)
		{
			oldValue ??= string.Empty;
			newValue ??= string.Empty;

			CompleteContent.Document ??= new FlowDocument { PagePadding = new Thickness(0) };
			IncompleteContent.Document ??= new FlowDocument { PagePadding = new Thickness(0) };

			if (Completed)
			{
				CompleteContent.Document = MarkdownParser.ParseDocument(newValue);
				IncompleteContent.Document.Blocks.Clear();
			}
			else
			{
				MarkdownParser.ParseDocumentStreaming(CompleteContent.Document, IncompleteContent.Document,
					oldValue, newValue, ref _accumulatedMarkdownLen);
			}

			CompleteContent.Visibility = CompleteContent.Document.Blocks.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
			IncompleteContent.Visibility = IncompleteContent.Document.Blocks.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
		}
		private void OnCompletedChanged(bool oldValue, bool newValue)
		{
			CompleteContent.Document ??= new FlowDocument { PagePadding = new Thickness(0) };
			IncompleteContent.Document ??= new FlowDocument { PagePadding = new Thickness(0) };

			if (newValue)
			{
				_accumulatedMarkdownLen = MarkdownText.Length;

				var incompleteBlocks = IncompleteContent.Document.Blocks.ToList();
				IncompleteContent.Document.Blocks.Clear();

				foreach (var block in incompleteBlocks)
					CompleteContent.Document.Blocks.Add(block);
				CompleteContent.Document.PagePadding = new Thickness(0);
			}
			else
			{
				_accumulatedMarkdownLen = 0;

				var completeBlocks = CompleteContent.Document.Blocks.ToList();
				CompleteContent.Document.Blocks.Clear();

				foreach (var block in completeBlocks)
					IncompleteContent.Document.Blocks.Add(block);
				IncompleteContent.Document.PagePadding = new Thickness(0);
			}

			CompleteContent.Visibility = CompleteContent.Document.Blocks.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
			IncompleteContent.Visibility = IncompleteContent.Document.Blocks.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
		}
	}
}