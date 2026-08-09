using System.ComponentModel;
using System.Text;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Agents.Memory;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Agents;
using LLTSharp;
using Material.Icons;

namespace LLMDesktopAssistant.Tools
{
	[ToolModule]
	public class MemoryToolModule : ToolModule
	{
		private readonly Chat _chat;
		private readonly IAgentManagementService _agentManager;
		private readonly IMemoryFactStore _memoryFactStore;

		public MemoryToolModule(Chat chat, IAgentManagementService agentManager,
			IMemoryFactStore memoryFactStore)
		{
			_chat = chat;
			_agentManager = agentManager;
			_memoryFactStore = memoryFactStore;

			AddTool(StoreAsync, new ToolInitializationInfo
			{
				Name = "memory-store",
				IsFixed = true,
				Description = """
					Stores a fact in the specified memory block with the given importance.
					""",
				Category = "memory"
			});

			AddTool(RetrieveAsync, new ToolInitializationInfo
			{
				Name = "memory-retrieve",
				IsFixed = true,
				Description = """
					Retrieves facts from the specified memory blocks (or all enabled blocks) by the given query.
					HyDE query is used to improve semantic matching and may be provided additionally.
					""",
				Category = "memory"
			});
		}

		public override IEnumerable<ToolInfo> GetTools()
		{
			if (!_chat.Settings.Memory.GetEffectiveMemoryOptions().EnableMemory)
				return [];
			return base.GetTools();
		}

		private async Task StoreAsync(
			[Description("The memory block where the fact should be stored")] string block,
			[Description("The fact to store in the memory block in user's language. Examples: 'User is vegetarian' or 'User likes pizza'")] string fact,
			[Description("The importance of the fact, from 0.0 (least important) to 1.0 (most important)")] double importance,
			ReactiveToolResult result,
			ToolExecutionContext ctx,
			[Description("Whether to supersede conflicting facts. false = append new fact, true = supersede conflicting fact")] bool? conflictSupersede = null,
			CancellationToken cancellationToken = default)
		{
			result.StatusIcon = MaterialIconKind.DatabaseAdd;
			result.StatusTitle = $"**{fact}** → *{block}*";

			var targetBlock = GetBlock(ctx, block, requireWriting: true, requireFacts: true);
			if (targetBlock is null)
			{
				result.ResultContent = $"Memory block '{block}' is not found or does not allow writing.";
				result.CompleteWithError();
				return;
			}

			try
			{
				int messageId = ctx.FindMessageId();

				var similarFacts = await _memoryFactStore.SearchAsync(targetBlock, fact, maxCount: 10, cancellationToken: cancellationToken);
				var highestScore = similarFacts.MaxBy(f => f.CosineScore ?? 0);

				if (highestScore is not null)
				{
					if (highestScore.CosineScore >= 0.9)
					{
						// Automatically supersede if the similarity is very high
						conflictSupersede ??= true;
					}
					else if (highestScore.CosineScore < 0.6)
					{
						conflictSupersede ??= false;
					}

					if (conflictSupersede is null)
					{
						var conflicts = string.Join(Environment.NewLine, similarFacts
							.Where(f => (f.CosineScore ?? 0) >= 0.6)
							.Select(f => $"- **{f.CosineScore:0.00}** [id: {f.Id}] {f.Text}"));

						result.StatusIcon = MaterialIconKind.DatabaseRefresh;
						result.ResultContent = $"""
							Conflict detected with similar facts (cosine score >= 0.6):
							{conflicts}

							Call tool again with conflictSupersede=true to supersede the most similar fact or conflictSupersede=false to append a new fact.
							""";
						result.CompleteWithSuccess();
						return;
					}
					else if (conflictSupersede is true)
					{
						var storedFact = await _memoryFactStore.SupersedeAsync(targetBlock, highestScore.Id, fact, _chat.ChatId, messageId, importance, cancellationToken);
						result.StatusIcon = MaterialIconKind.DatabaseEdit;
						result.ResultContent = $"Fact stored with supersede successfully. ID: {storedFact.Id}, supersed fact: {highestScore.Text}";
						result.CompleteWithSuccess();
						return;
					}
					else
					{
						var storedFact = await _memoryFactStore.StoreAsync(targetBlock, fact, _chat.ChatId, messageId, importance, cancellationToken);
						result.StatusIcon = MaterialIconKind.DatabaseCheck;
						result.ResultContent = $"Fact stored successfully. ID: {storedFact.Id}";
						result.CompleteWithSuccess();
						return;
					}
				}
				else
				{
					var storedFact = await _memoryFactStore.StoreAsync(targetBlock, fact, _chat.ChatId, messageId, importance, cancellationToken);
					result.StatusIcon = MaterialIconKind.DatabaseCheck;
					result.ResultContent = $"Fact stored successfully. ID: {storedFact.Id}";
					result.CompleteWithSuccess();
					return;
				}
			}
			catch (Exception ex)
			{
				result.StatusIcon = MaterialIconKind.DatabaseOff;
				result.ResultContent = $"Failed to store fact in memory block '{block}'. Error: {ex.Message}";
				result.CompleteWithError();
				return;
			}
		}

		private async Task RetrieveAsync(
			[Description("The memory blocks where the fact should be retrieved. Use null to retrieve from all enabled memory blocks")] string[]? blocks,
			[Description("The query to retrieve by in user's language")] string query,
			[Description("The hypotetical query to retrieve by in user's language. Examples: 'User is vegetarian' or 'User likes pizza'")] string hyDe,
			ReactiveToolResult result,
			ToolExecutionContext ctx,
			CancellationToken cancellationToken = default)
		{
			result.StatusIcon = MaterialIconKind.DatabaseSearch;
			result.StatusTitle = $"**{query}**";

			var blocksToSearch = GetBlocks(ctx, blocks, requireReading: true, requireWriting: false, requireFacts: true);

			if (blocksToSearch.Count == 0)
			{
				result.ResultContent = "No memory blocks to search in.";
				result.CompleteWithError();
				return;
			}

			try
			{
				var perBlockResults = await Task.WhenAll(blocksToSearch.Select(async block =>
				{
					var queryResults = await _memoryFactStore.SearchAsync(block, query, maxCount: 10, cancellationToken: cancellationToken);
					var hyDeResults = string.IsNullOrWhiteSpace(hyDe)
						? []
						: await _memoryFactStore.SearchAsync(block, hyDe, maxCount: 10, cancellationToken: cancellationToken);
					return (block, MergeResults(queryResults, hyDeResults));
				}));

				var sb = new StringBuilder();
				int totalFound = 0;

				foreach (var (block, facts) in perBlockResults)
				{
					if (facts.Count == 0)
						continue;

					totalFound += facts.Count;
					sb.AppendLine($"### {block.Name}");
					foreach (var fact in facts)
					{
						var cosineScore = fact.CosineScore?.ToString("0.00") ?? "unknown";
						var bm25Score = fact.Bm25Score?.ToString("0.00") ?? "unknown";
						sb.AppendLine($"- **{fact.Text}** [id: {fact.Id}] (cosine score: {cosineScore}, bm25 score: {bm25Score})");
					}
					sb.AppendLine();
				}

				if (totalFound == 0)
				{
					result.ResultContent = "No matching facts found.";
					result.CompleteWithSuccess();
					return;
				}

				result.UseMarkdown = true;
				result.ResultContent = sb.ToString();
				result.CompleteWithSuccess();
			}
			catch (Exception ex)
			{
				result.StatusIcon = MaterialIconKind.DatabaseOff;
				result.ResultContent = $"Failed to retrieve facts. Error: {ex.Message}";
				result.CompleteWithError();
				return;
			}
		}

		private MemoryBlock? GetBlock(ToolExecutionContext ctx, string name, bool requireWriting, bool requireFacts)
		{
			var agent = _agentManager.GetAgentDescriptor(ctx.Message.SenderAgentId);
			return GetBlocks(ctx, [name], requireReading: false, requireWriting: requireWriting, requireFacts: requireFacts).FirstOrDefault();
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

		private static List<MemoryFactResult> MergeResults(MemoryFactResult[] queryResults, MemoryFactResult[] hyDeResults)
		{
			var merged = new Dictionary<int, MemoryFactResult>();
			foreach (var fact in queryResults.Concat(hyDeResults))
			{
				if (!merged.TryGetValue(fact.Id, out var existing) || (fact.RrfScore ?? 0) > (existing.RrfScore ?? 0))
					merged[fact.Id] = fact;
			}

			return merged.Values
				.OrderByDescending(f => f.RrfScore ?? f.CosineScore ?? f.Bm25Score ?? 0)
				.ToList();
		}
	}
}
