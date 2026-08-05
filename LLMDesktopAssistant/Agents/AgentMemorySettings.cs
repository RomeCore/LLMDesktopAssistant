using LLMDesktopAssistant.SourceGenerators;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Agents
{
	[SettingsRoute(nameof(ChatAgentDescriptor.Memory))]
	public partial class AgentMemorySettings : AgentSettingsCategoryBase
	{
		private bool _enableMemory = false;
		/// <summary>
		/// Enables or disables the agent memory feature.
		/// </summary>
		[InheritedChatAgentSetting]
		public bool EnableMemory
		{
			get => _enableMemory;
			set => SetProperty(ref _enableMemory, value);
		}

		private readonly RangeObservableCollection<MemoryBlockAttachment> _blocks = [];
		/// <summary>
		/// Memory that attached to the agent.
		/// </summary>
		[InheritedChatAgentSetting]
		public RangeObservableCollection<MemoryBlockAttachment> Blocks
		{
			get => _blocks;
			set => _blocks.Reset(value);
		}
	}
}