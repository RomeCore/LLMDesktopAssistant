using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Agents
{
	public class AgentsConfiguration : SettingsObject
	{
		private RangeObservableCollection<AgentDescriptor> _agents = [];
		public RangeObservableCollection<AgentDescriptor> Agents
		{
			get => _agents;
			set => _agents.Reset(value);
		}
	}
}