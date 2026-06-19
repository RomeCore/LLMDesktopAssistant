using System;
using System.Collections.Generic;
using System.Text;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;

namespace LLMDesktopAssistant.Prompting.ContextExpanders
{
	[ChatService(typeof(IPromptSystemContextExpander))]
	public class WorkingDirectoryContextExpander(Chat chat) : IPromptSystemContextExpander
	{
		public void ExpandPromptContext(Dictionary<string, object?> context)
		{
			context["working_directory"] = chat.Settings.Environment.GetWorkingDirectory();
		}
	}
}