using LLMDesktopAssistant.Agents.Memory;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Agents;

namespace LLMDesktopAssistant.Tools.Implementations.Memory
{
	/// <summary>
	/// Base class for memory-related tool modules. Provides access to the memory blocks of the
	/// sender agent and hides all memory tools when memory is disabled in the chat settings.
	/// </summary>
	public abstract class MemoryToolModuleBase : ToolModule
	{
		private readonly Chat _chat;
		private readonly IAgentManagementService _agentManager;

		/// <summary>
		/// Gets the chat instance where the tools are executed.
		/// </summary>
		protected Chat Chat => _chat;

		/// <summary>
		/// Gets the agent management service used to resolve agent memory attachments.
		/// </summary>
		protected IAgentManagementService AgentManager => _agentManager;

		/// <summary>
		/// Initializes a new instance of the <see cref="MemoryToolModuleBase"/> class.
		/// </summary>
		/// <param name="chat">The chat instance where the tools are executed.</param>
		/// <param name="agentManager">The agent management service used to resolve agent memory attachments.</param>
		protected MemoryToolModuleBase(Chat chat, IAgentManagementService agentManager)
		{
			_chat = chat;
			_agentManager = agentManager;
		}

		/// <inheritdoc/>
		public override IEnumerable<ToolInfo> GetTools()
		{
			var memoryOpts = _chat.Settings.Memory.GetEffectiveMemoryOptions();
			if (memoryOpts.EnableMemory && memoryOpts.ManualControlEnabled)
				return base.GetTools();
			return [];
		}

		/// <summary>
		/// Resolves the memory blocks attached to the sender agent of the specified context,
		/// optionally filtered by names and access requirements.
		/// </summary>
		/// <param name="ctx">The tool execution context.</param>
		/// <param name="names">The block names to filter by, or <see langword="null"/> to include all attached blocks.</param>
		/// <param name="requireReading">Whether to include only blocks that allow reading.</param>
		/// <param name="requireWriting">Whether to include only blocks that allow writing.</param>
		/// <param name="requireFacts">Whether to include only blocks with facts enabled.</param>
		/// <param name="requireLogs">Whether to include only blocks with logs enabled.</param>
		/// <returns>The list of resolved memory blocks.</returns>
		protected List<MemoryBlock> GetBlocks(ToolExecutionContext ctx, string[]? names, bool requireReading, bool requireWriting,
			bool requireFacts = false, bool requireLogs = false)
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
			if (requireLogs)
				attachments = attachments.Where(b => b.Reference.Object!.LogsEnabled);

			return attachments.Select(b => b.Reference.Object!).ToList();
		}
	}
}
