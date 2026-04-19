using LLMDesktopAssistant.Services;
using RCLargeLanguageModels.Tools;
using System.ComponentModel;

namespace LLMDesktopAssistant.ToolModules.Implementations
{
	[ToolModule]
	public class GeneralToolModule : ToolModule
	{
		public GeneralToolModule()
		{
			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(Copy, "copy", "Copies a piece of text to the clipboard, use when neccessary."),
				Category = "general"
			});
			
			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(GenerateGUID, "generate_GUID", "Generates a globally unique identifier (GUID)."),
				Category = "general"
			});
			
			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(GenerateRandomInteger, "generate_random_integer", "Generates a random integer number."),
				Category = "general"
			});
			
			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(GenerateRandomFloat, "generate_random_float", "Generates a random floating-point number."),
				Category = "general"
			});
		}

		private ToolResult Copy([Description("Text to copy")] string text)
		{
			// TODO: Implement the logic for copying text to the clipboard.
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