using LLMDesktopAssistant.Agents.Memory;
using LLMDesktopAssistant.LLM.Settings;
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

		/// <summary>
		/// Returns the enabled memory blocks with valid settings (facts or logs enabled, and allows reading or writing).
		/// </summary>
		/// <param name="settings">The chat settings for getting effective settings.</param>
		/// <returns>The enabled memory blocks with valid settings.</returns>
		public IEnumerable<(MemoryBlock Block, MemoryBlockAttachment Attachment)> GetEnabledBlocks(ChatSettings settings)
		{
			var effectiveBlocks = GetEffectiveBlocks(settings);
			return effectiveBlocks
				.Select(b => (Block: b.Reference.Object!, Attachment: b))
				.Where(b => b.Block != null && b.Attachment.Enabled
					&& (b.Block.FactsEnabled || b.Block.LogsEnabled)
					&& (b.Attachment.AllowsReading() || b.Attachment.AllowsWriting()));
		}
	}
}