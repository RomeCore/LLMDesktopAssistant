using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Tools;

namespace LLMDesktopAssistant.Desktop.ToolModules
{
	// [ToolModule(chatScoped: true)]
	public class ShellToolModule : ToolModule
	{
		private Chat _chat;
		private WorkingDirectoryAccessService _wdAccess;

		public ShellToolModule(Chat chat, WorkingDirectoryAccessService wdAccess)
		{
			_chat = chat;
			_wdAccess = wdAccess;
		}
	}
}