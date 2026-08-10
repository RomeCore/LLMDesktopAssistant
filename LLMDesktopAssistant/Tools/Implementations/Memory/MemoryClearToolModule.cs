using System.ComponentModel;
using LLMDesktopAssistant.Agents.Memory;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Agents;
using Material.Icons;

namespace LLMDesktopAssistant.Tools.Implementations.Memory
{
	[ToolModule]
	public class MemoryClearToolModule : ToolModule
	{
		private readonly Chat _chat;
		private readonly IAgentManagementService _agentManager;
		private readonly IMemoryFactStore _memoryFactStore;
		private readonly IMemoryLogStore _memoryLogStore;

		public MemoryClearToolModule(Chat chat, IAgentManagementService agentManager,
			IMemoryFactStore memoryFactStore, IMemoryLogStore memoryLogStore)
		{
			_chat = chat;
			_agentManager = agentManager;
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

		public override IEnumerable<ToolInfo> GetTools()
		{
			if (!_chat.Settings.Memory.GetEffectiveMemoryOptions().EnableMemory)
				return [];
			return base.GetTools();
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

		private List<MemoryBlock> GetBlocks(ToolExecutionContext ctx, string[]? names, bool requireReading, bool requireWriting, bool requireFacts)
		{
			var agent = _agentManager.GetAgentDescriptor(ctx.Message.SenderAgentId);
			var attachments = agent.Memory.GetEffectiveBlocks(_chat.Settings)
				.Where(b => b.Enabled && b.Reference.Object != null);

			if (names is not null)
			{
				var namesSet = names.ToHashSet();
				attachments = attachments.Where(b => namesSet.Contains(b.Reference.Object!.Name));
			}

			if (requireReading)
				attachments = attachments.Where(b => b.AllowsReading());
			if (requireWriting)
				attachments = attachments.Where(b => b.AllowsWriting());
			if (requireFacts)
				attachments = attachments.Where(b => b.Reference.Object!.FactsEnabled);

			return attachments.Select(b => b.Reference.Object!).ToList();
		}
	}
}
