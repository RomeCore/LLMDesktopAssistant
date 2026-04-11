using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using Markdig.Extensions.Mathematics;
using Markdig.Renderers;
using Markdig.Renderers.Wpf;
using Markdig.Wpf;
using WpfMath.Controls;

namespace LLMDesktopAssistant.Core.Utils.Markdown
{
	public class MathInlineRenderer : WpfObjectRenderer<MathInline>
	{
		protected override void Write(WpfRenderer renderer, MathInline obj)
		{
			var formula = obj.Content.Text[obj.Content.Start..(obj.Content.End + 1)];
			var mathElement = CreateCopyableFormulaControl(formula);

			if (mathElement.HasError)
			{
				renderer.WriteInline(new Run(formula));
			}
			else
			{
				var inline = new InlineUIContainer(mathElement);
				renderer.WriteInline(inline);
			}
		}

		/// <summary>
		/// Creates a <see cref="FormulaControl"/> that displays the given formula copyes the formula to the clipboard when clicked.
		/// </summary>
		/// <param name="formula">The formula to display.</param>
		/// <returns>The created <see cref="FormulaControl"/>.</returns>
		public static FormulaControl CreateCopyableFormulaControl(string formula)
		{
			var control = new FormulaControl
			{
				Formula = formula,
				Cursor = Cursors.Hand
			};

			control.MouseLeftButtonDown += (sender, e) =>
			{
				Clipboard.SetText(formula);
			};

			return control;
		}
	}
}