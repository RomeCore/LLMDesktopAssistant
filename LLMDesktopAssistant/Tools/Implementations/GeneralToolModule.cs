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
			AddTool(Copy,
				new ToolInitializationInfo
				{
					Name = "copy",
					Description = "Copies a piece of text to the clipboard, use when neccessary.",
					Category = "general"
				});
		}

		private ToolResult Copy([Description("Text to copy")] string text)
		{
			_ = App.MainTopLevel.Clipboard!.SetTextAsync(text);
			return new ToolResult("Text copied to clipboard.");
		}
	}
}