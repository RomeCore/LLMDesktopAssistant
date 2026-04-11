using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Markdig.Extensions.Mathematics;
using Markdig.Renderers;
using Markdig.Renderers.Wpf;
using Markdig.Wpf;

namespace LLMDesktopAssistant.Core.Utils.Markdown
{
	public class MathBlockRenderer : WpfObjectRenderer<MathBlock>
	{
		protected override void Write(WpfRenderer renderer, MathBlock obj)
		{
			var sb = new StringBuilder();

			foreach (var line in obj.Lines.Lines ?? [])
			{
				var slice = line.Slice;
				if (string.IsNullOrEmpty(slice.Text))
					continue;
				var formulaLine = slice.Text[slice.Start..(slice.End + 1)];
				if (string.IsNullOrEmpty(formulaLine))
					continue;

				sb.AppendLine(formulaLine);
			}

			var formula = sb.ToString().Trim();
			var control = MathInlineRenderer.CreateCopyableFormulaControl(formula);

			if (control.HasError)
			{
				var paragraph = new Paragraph(new Run(formula));
				paragraph.SetResourceReference(Paragraph.StyleProperty, Styles.ParagraphStyleKey);
				renderer.Push(paragraph);
				renderer.Pop();
			}
			else
			{
				var block = new BlockUIContainer(control);
				renderer.Push(block);
				renderer.Pop();
			}
		}
	}
}