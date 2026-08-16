using System.ComponentModel;
using Avalonia.Input.Platform;
using RCLargeLanguageModels.Tools;
using LLMDesktopAssistant.Localization;

namespace LLMDesktopAssistant.Tools.Implementations
{
	[ToolModule]
	public class GeneralToolModule : ToolModule
	{
		public GeneralToolModule()
		{
			AddTool(new ToolInitializationInfo
			{
				Executor = ClipboardCopy,
				Name = "clipboard-copy",
				Description = "Copies a piece of text to the clipboard, use when neccessary.",
				TitleKey = Locale.GetKey("tool.name.clipboard-copy"),
				DescriptionKey = Locale.GetKey("tool.description.clipboard-copy"),
				CategoryKey = Locale.GetKey("tool.category.general"),
				DefaultExpectedBehaviour = ToolBehaviour.ClipboardWrite
			});

			AddTool(new ToolInitializationInfo
			{
				Executor = ClipboardRead,
				Name = "clipboard-read",
				Description = "Reads the current content of the clipboard.",
				TitleKey = Locale.GetKey("tool.name.clipboard-read"),
				DescriptionKey = Locale.GetKey("tool.description.clipboard-read"),
				CategoryKey = Locale.GetKey("tool.category.general"),
				DefaultExpectedBehaviour = ToolBehaviour.ClipboardRead
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