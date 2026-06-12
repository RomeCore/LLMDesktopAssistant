using System;
using System.Collections.Generic;
using System.Text;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Tools.Implementations;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// The settings that related to tools implementations in the chat application.
	/// </summary>
	public class ChatToolsSettings : NotifyPropertyChanged
	{
		private ToolBehaviour _autoApproveBehaviours = ToolBehaviour.None;
		/// <summary>
		/// The behaviour of tools that will be automatically approved.
		/// </summary>
		public ToolBehaviour AutoApproveBehaviours
		{
			get => _autoApproveBehaviours;
			set => SetProperty(ref _autoApproveBehaviours, value);
		}

		private ToolBehaviour _disallowedBehaviours = ToolBehaviour.None;
		/// <summary>
		/// The behaviour of tools that will be disallowed.
		/// </summary>
		public ToolBehaviour DisallowedBehaviours
		{
			get => _disallowedBehaviours;
			set => SetProperty(ref _disallowedBehaviours, value);
		}

	}
}