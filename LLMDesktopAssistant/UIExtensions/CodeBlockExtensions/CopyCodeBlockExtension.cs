using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.Input;
using LiveMarkdown.Avalonia;
using Material.Icons;

namespace LLMDesktopAssistant.UIExtensions.CodeBlockExtensions
{
	[CodeBlockExtension]
	public class CopyCodeBlockExtension : CodeBlockExtension
	{
		public CopyCodeBlockExtension(CodeBlock codeBlock)
		{
			Icon = MaterialIconKind.ContentCopy;

			Command = new RelayCommand(() =>
			{
				App.MainTopLevel.Clipboard!.SetTextAsync(codeBlock.Code);
			}, () => App.MainTopLevel.Clipboard != null);
		}
	}
}