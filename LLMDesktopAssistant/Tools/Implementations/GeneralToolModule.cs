using Avalonia.Input.Platform;
using LLMDesktopAssistant.Services;
using RCLargeLanguageModels.Tools;
using System.ComponentModel;

namespace LLMDesktopAssistant.Tools.Implementations
{
	[ToolModule]
	public class GeneralToolModule : ToolModule
	{
		public GeneralToolModule()
		{
			AddTool(ClipboardCopy,
				new ToolInitializationInfo
				{
					Name = "clipboard-copy",
					Description = "Copies a piece of text to the clipboard, use when neccessary.",
					Category = "general",
					DefaultExpectedBehaviour = ToolBehaviour.ClipboardAccess
				});

			AddTool(ClipboardRead,
				new ToolInitializationInfo
				{
					Name = "clipboard-read",
					Description = "Reads the current content of the clipboard.",
					Category = "general",
					DefaultExpectedBehaviour = ToolBehaviour.ClipboardAccess
				});
		}

		private ToolResult ClipboardCopy([Description("Text to copy")] string text)
		{
			_ = App.MainTopLevel.Clipboard!.SetTextAsync(text);
			return new ToolResult("Text copied to clipboard.");
		}

		private async Task<ToolResult> ClipboardRead()
		{
			var content = await App.MainTopLevel.Clipboard!.TryGetTextAsync();
			return new ToolResult(content ?? "<Clipboard is empty>");
		}
	}
}