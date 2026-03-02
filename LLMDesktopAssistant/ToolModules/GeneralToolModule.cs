using System.ComponentModel;
using System.Windows.Forms;
using LLMDesktopAssistant.Modules;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.ToolModules
{
	[Module]
	public class GeneralToolModule : ToolModule
	{
		private readonly List<FunctionTool> _tools;

		public GeneralToolModule()
		{
			var printTool = FunctionTool.From(Print, "general-print", "Prints a message to the console.");
			var copyTool = FunctionTool.From(Copy, "general-copy", "Copies a piece of text to the clipboard.");
			var genGUIDTool = FunctionTool.From(GenerateGUID, "general-generateGUID", "Generates a globally unique identifier (GUID).");
			var genRandIntTool = FunctionTool.From(GenerateRandomInteger, "general-generateRandomInteger", "Generates a random integer number.");
			var genRandFloatTool = FunctionTool.From(GenerateRandomFloat, "general-GenerateRandomFloat", "Generates a random floating-point number.");
			
			_tools = [printTool, copyTool, genGUIDTool, genRandIntTool, genRandFloatTool];
		}

		private ToolResult Print([Description("Message to print")] string message)
		{
			Console.WriteLine(message);
			return new ToolResult("Success!");
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
			[Description("The maximum exclusive value")] long maxValue)
		{
			var value = Random.Shared.NextInt64(minValue, maxValue);
			return new ToolResult(value.ToString());
		}

		private ToolResult GenerateRandomFloat([Description("The minimum inclusive value")] double minValue,
			[Description("The maximum inclusive value")] double maxValue)
		{
			var range = maxValue - minValue;
			var value = Random.Shared.NextDouble() * range + minValue;
			return new ToolResult(value.ToString());
		}

		public override IEnumerable<ITool> GetTools()
		{
			return _tools;
		}
	}
}