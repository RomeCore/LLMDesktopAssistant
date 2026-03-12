using System.ComponentModel;
using System.Windows.Forms;
using LLMDesktopAssistant.Modules;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.ToolModules
{
	[Module]
	public class GeneralToolModule : ToolModule
	{
		public GeneralToolModule()
		{
			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(Copy, "general-copy", "Copies a piece of text to the clipboard, use when neccessary.")
			});
			
			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(GenerateGUID, "general-generate_GUID", "Generates a globally unique identifier (GUID).")
			});
			
			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(GenerateRandomInteger, "general-generate_random_integer", "Generates a random integer number.")
			});
			
			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(GenerateRandomFloat, "general-generate_random_float", "Generates a random floating-point number.")
			});
		}

		private ToolResult Copy([Description("Text to copy")] string text)
		{
			App.Current.Dispatcher.Invoke(() =>
			{
				Clipboard.SetText(text);
			});
			return new ToolResult("Text copied to clipboard.");
		}

		private ToolResult GenerateGUID()
		{
			var value = Guid.NewGuid();
			return new ToolResult(value.ToString());
		}

		private ToolResult GenerateRandomInteger([Description("The minimum inclusive value")] long minValue,
			[Description("The maximum inclusive value")] long maxValue)
		{
			var value = Random.Shared.NextInt64(minValue, maxValue + 1);
			return new ToolResult(value.ToString());
		}

		private ToolResult GenerateRandomFloat([Description("The minimum inclusive value")] double minValue,
			[Description("The maximum inclusive value")] double maxValue)
		{
			var range = maxValue - minValue;
			var value = Random.Shared.NextDouble() * range + minValue;
			return new ToolResult(value.ToString());
		}
	}
}