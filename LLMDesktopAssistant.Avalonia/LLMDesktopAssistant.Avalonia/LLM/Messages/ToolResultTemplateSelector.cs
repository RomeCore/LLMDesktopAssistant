using System.Windows;
using System.Windows.Controls;

namespace LLMDesktopAssistant.Avalonia.LLM.Messages
{
	public class ToolResultTemplateSelector : DataTemplateSelector
	{
		public DataTemplate? MarkdownTemplate { get; set; }
		public DataTemplate? PlainTextTemplate { get; set; }

		public int Threshold { get; set; } = 1000;

		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			if (item is string text)
			{
				if (text.Length < Threshold)
					return MarkdownTemplate!;
				else
					return PlainTextTemplate!;
			}

			return base.SelectTemplate(item, container);
		}
	}
}