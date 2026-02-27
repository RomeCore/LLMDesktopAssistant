using System.ComponentModel;
using System.Windows.Forms;
using LLMDesktopAssistant.Modules;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.ToolModules
{
	[Module]
	public class ExampleToolModule : ToolModule
	{
		private readonly FunctionTool _printTool, _copyTool, _genGUIDTool;

		public ExampleToolModule()
		{
			_printTool = FunctionTool.From(Print, "print", "Prints a message to the console.");
			_copyTool = FunctionTool.From(Copy, "copy", "Copies a piece of text to the clipboard.");
			_genGUIDTool = FunctionTool.From(GenerateGUID, "generateGUID", "Generates a globally unique identifier (GUID).");
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
			return new ToolResult(Guid.NewGuid().ToString());
		}

		public override IEnumerable<ITool> GetTools()
		{
			return [_printTool, _copyTool, _genGUIDTool];
		}
	}
}