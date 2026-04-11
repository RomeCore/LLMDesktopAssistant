using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using Markdig.Renderers;
using Markdig.Renderers.Wpf;
using Markdig.Syntax.Inlines;
using Markdig.Wpf;

namespace LLMDesktopAssistant.Core.Utils.Markdown
{
	/// <summary>
	/// Renders a <see cref="LinkInline"/> as an <see cref="Hyperlink"/>.
	/// </summary>
	public class LinkInlineRenderer : WpfObjectRenderer<LinkInline>
	{
		private static void OpenLink(string url)
		{
			if (Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
			{
				System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
			}
		}

		private static ICommand OpenLinkCommand { get; }

		static LinkInlineRenderer()
		{
			OpenLinkCommand = new RelayCommand<string>(parameter => OpenLink(parameter ?? string.Empty));
		}

		protected override void Write(WpfRenderer renderer, LinkInline link)
		{
			ArgumentNullException.ThrowIfNull(renderer);
			ArgumentNullException.ThrowIfNull(link);

			string url = (link.GetDynamicUrl != null ? link.GetDynamicUrl() ?? link.Url : link.Url) ?? string.Empty;
			if (!Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
			{
				url = "#";
			}

			if (link.IsImage)
			{
				ControlTemplate controlTemplate = new ControlTemplate();
				FrameworkElementFactory frameworkElementFactory = new FrameworkElementFactory(typeof(Image));
				frameworkElementFactory.SetValue(Image.SourceProperty, new BitmapImage(new Uri(url, UriKind.RelativeOrAbsolute)));
				frameworkElementFactory.SetResourceReference(FrameworkContentElement.StyleProperty, Styles.ImageStyleKey);
				controlTemplate.VisualTree = frameworkElementFactory;
				Button childUIElement = new Button
				{
					Template = controlTemplate,
					Command = OpenLinkCommand,
					CommandParameter = url,
					Width = 200,
					Height = 200
				};
				renderer.WriteInline(new InlineUIContainer(childUIElement));
			}
			else
			{
				Hyperlink hyperlink = new Hyperlink
				{
					Command = OpenLinkCommand,
					CommandParameter = url,
					NavigateUri = new Uri(url, UriKind.RelativeOrAbsolute),
					ToolTip = url
				};
				hyperlink.SetResourceReference(FrameworkContentElement.StyleProperty, Styles.HyperlinkStyleKey);
				renderer.Push(hyperlink);
				renderer.WriteChildren(link);
				renderer.Pop();
			}
		}
	}
}