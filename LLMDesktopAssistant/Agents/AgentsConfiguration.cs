using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Agents
{
	public class AgentsConfiguration : SettingsObject
	{
		private RangeObservableCollection<ChatAgentDescriptor> _agents = [];
		public RangeObservableCollection<ChatAgentDescriptor> Agents
		{
			get => _agents;
			set => _agents.Reset(value);
		}
	}
}