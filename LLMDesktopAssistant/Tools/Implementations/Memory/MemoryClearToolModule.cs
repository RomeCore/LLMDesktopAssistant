using System.ComponentModel;
using LLMDesktopAssistant.Agents.Memory;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Agents;
using Material.Icons;

namespace LLMDesktopAssistant.Tools.Implementations.Memory
{
	[ToolModule]
	public class MemoryClearToolModule : MemoryToolModuleBase
	{
		private readonly IMemoryFactStore _memoryFactStore;
		private readonly IMemoryLogStore _memoryLogStore;

		public MemoryClearToolModule(Chat chat, IAgentManagementService agentManager,
			IMemoryFactStore memoryFactStore, IMemoryLogStore memoryLogStore)
			: base(chat, agentManager)
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
				DefaultExpectedBehaviour = ToolBehaviour.MemoryAccess | ToolBehaviour.MemoryClear,
				Category = "memory"
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

			var targetBlock = GetBlocks(ctx, [block], requireReading: false, requireWriting: true, requireFacts: false).FirstOrDefault();
			if (targetBlock is null)
			{
				result.ResultContent = $"Memory block '{block}' is not found or does not allow writing.";
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
