using System.ComponentModel;
using System.Windows.Forms;
using LLMDesktopAssistant.Modules;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.ToolModules
{
	[Module]
	public class ExampleToolModule : ToolModule
	{
		private readonly List<FunctionTool> _tools;

		public ExampleToolModule()
		{
			var printTool = FunctionTool.From(Print, "print", "Prints a message to the console.");
			var copyTool = FunctionTool.From(Copy, "copy", "Copies a piece of text to the clipboard.");
			var genGUIDTool = FunctionTool.From(GenerateGUID, "generateGUID", "Generates a globally unique identifier (GUID).");
			
			_tools = [printTool, copyTool, genGUIDTool];
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
			return _tools;
		}
	}
}