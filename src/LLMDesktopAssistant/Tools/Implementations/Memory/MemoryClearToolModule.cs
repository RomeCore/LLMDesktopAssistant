using System.ComponentModel;
using LLMDesktopAssistant.Agents.Memory;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.LLM.Services.Agents;
using Material.Icons;
using LLMDesktopAssistant.Localization;

namespace LLMDesktopAssistant.Tools.Implementations.Memory
{
	[ToolModule]
	public class MemoryClearToolModule : MemoryToolModuleBase
	{
		private readonly IMemoryFactStore _memoryFactStore;
		private readonly IMemoryLogStore _memoryLogStore;

		/// <summary>
		/// Initializes a new instance of the <see cref="MemoryClearToolModule"/> class.
		/// </summary>
		/// <param name="chat">The chat instance where the tools are executed.</param>
		/// <param name="chatSettings">The settings service of the chat.</param>
		/// <param name="agentManager">The agent management service used to resolve agent memory attachments.</param>
		/// <param name="memoryFactStore">The store used to clear semantic memory facts.</param>
		/// <param name="memoryLogStore">The store used to clear episodic memory logs.</param>
		public MemoryClearToolModule(Chat chat, IChatSettingsService chatSettings, IAgentManagementService agentManager,
			IMemoryFactStore memoryFactStore, IMemoryLogStore memoryLogStore)
			: base(chat, chatSettings, agentManager)
		{
			_memoryFactStore = memoryFactStore;
			_memoryLogStore = memoryLogStore;

			AddTool(ClearAsync, new ToolInitializationInfo
			{
				Name = "memory-clear",
				IsFixed = false,
				Description = """
					Clears the stored facts and/or episodic logs from the specified memory block.
					The block itself and its configuration are preserved.
					""",
				DefaultExpectedBehaviour = ToolBehaviour.SemanticMemoryClear,
				TitleKey = Locale.GetKey("tool.name.memory-clear"),
				DescriptionKey = Locale.GetKey("tool.description.memory-clear"),
				CategoryKey = Locale.GetKey("tool.category.memory")
			});
		}

		private async Task ClearAsync(
			[Description("The memory block to clear")] string block,
			ReactiveToolResult result,
			ToolExecutionContext ctx,
			[Description("Whether to clear the semantic facts. Defaults to true")] bool clearFacts = true,
			[Description("Whether to clear the episodic logs. Defaults to false")] bool clearLogs = false,
			CancellationToken cancellationToken = default)
		{
			result.StatusIcon = MaterialIconKind.DatabaseRefresh;
			result.StatusTitle = $"**{block}**";

			var (targetBlock, error) = GetBlock(ctx, block, requireReading: false, requireWriting: true);
			if (targetBlock is null)
			{
				result.ResultContent = error ?? $"Memory block '{block}' is not available.";
				result.CompleteWithError();
				return;
			}

			try
			{
				int removedFacts = 0;
				if (clearFacts && targetBlock.FactsEnabled)
					removedFacts = await _memoryFactStore.ClearAsync(targetBlock, cancellationToken);

				int removedLogs = 0;
				if (clearLogs && targetBlock.LogsEnabled)
					removedLogs = await _memoryLogStore.ClearAsync(targetBlock, cancellationToken);

				result.StatusIcon = MaterialIconKind.DatabaseCheck;
				result.ResultContent = $"Memory block '{block}' cleared. Removed facts: {removedFacts}, removed logs: {removedLogs}.";
				result.CompleteWithSuccess();
			}
			catch (Exception ex)
			{
				result.StatusIcon = MaterialIconKind.DatabaseOff;
				result.ResultContent = $"Failed to clear memory block '{block}'. Error: {ex.Message}";
				result.CompleteWithError();
			}
		}
	}
}
