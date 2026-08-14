using System.ComponentModel;
using System.Text;
using LLMDesktopAssistant.Agents.Memory;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Agents;
using Material.Icons;

namespace LLMDesktopAssistant.Tools.Implementations.Memory
{
	[ToolModule]
	public class MemoryFactToolModule : MemoryToolModuleBase
	{
		private readonly IMemoryFactStore _memoryFactStore;

		public MemoryFactToolModule(Chat chat, IAgentManagementService agentManager,
			IMemoryFactStore memoryFactStore)
			: base(chat, agentManager)
		{
			_memoryFactStore = memoryFactStore;

			AddTool(StoreAsync, new ToolInitializationInfo
			{
				Name = "memory-store_fact",
				IsFixed = true,
				Description = """
					Stores a fact in the specified memory block with the given importance.
					""",
				DefaultExpectedBehaviour = ToolBehaviour.SemanticMemoryWrite,
				Category = "memory"
			});

			AddTool(RetrieveAsync, new ToolInitializationInfo
			{
				Name = "memory-retrieve_fact",
				IsFixed = true,
				Description = """
					Retrieves facts with their IDs from the specified memory blocks (or all enabled blocks) by the given query.
					HyDE query is used to improve semantic matching and may be provided additionally.
					""",
				DefaultExpectedBehaviour = ToolBehaviour.SemanticMemoryRead,
				Category = "memory"
			});

			AddTool(ForgetAsync, new ToolInitializationInfo
			{
				Name = "memory-forget_fact",
				IsFixed = false,
				Description = """
					Forgets (deletes) a fact from the specified memory block by its ID.
					By default performs a soft delete: the fact is marked as Deleted and can be restored later.
					Use mode="hard" to remove the fact permanently.
					""",
				DefaultExpectedBehaviour = ToolBehaviour.SemanticMemoryDelete,
				Category = "memory"
			});
		}

		private async Task StoreAsync(
			[Description("The memory block where the fact should be stored")] string block,
			[Description("The fact to store in the memory block in user's language. Examples: 'User is vegetarian' or 'User likes pizza'")] string fact,
			[Description("The importance of the fact, from 0.0 (least important) to 1.0 (most important)")] double importance,
			ReactiveToolResult result,
			ToolExecutionContext ctx,
			[Description("The ID of the fact to supersede. If not provided, conflicts will be reported. If 0, the fact will be stored without superseding any existing facts.")] int? supersedeId = null,
			CancellationToken cancellationToken = default)
		{
			result.StatusIcon = MaterialIconKind.DatabaseAdd;
			result.StatusTitle = $"**{fact}** → *{block}*";

			var targetBlock = GetBlocks(ctx, [block], requireReading: false, requireWriting: true, requireFacts: true).FirstOrDefault();
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
						supersedeId ??= highestScore.Id;
					}
					else if (highestScore.CosineScore < 0.6)
					{
						supersedeId ??= 0;
					}

					if (supersedeId is null)
					{
						var conflicts = string.Join(Environment.NewLine, similarFacts
							.Where(f => (f.CosineScore ?? 0) >= 0.6)
							.Select(f => $"- **{f.CosineScore:0.00}** [id: {f.Id}] {f.Text}"));

						result.StatusIcon = MaterialIconKind.DatabaseRefresh;
						result.ResultContent = $"""
							Conflict detected with similar facts (cosine score >= 0.6):
							{conflicts}

							Call tool again with supersedeId=<fact id> to supersede the desired fact or supersedeId=0 to append a new fact.
							""";
						result.CompleteWithSuccess();
						return;
					}
					else
					{
						if (supersedeId == 0)
						{
							var storedFact = await _memoryFactStore.StoreAsync(targetBlock, fact, Chat.ChatId, messageId, importance, cancellationToken);
							result.StatusIcon = MaterialIconKind.DatabaseCheck;
							result.ResultContent = $"Fact stored successfully. ID: {storedFact.Id}";
							result.CompleteWithSuccess();
							return;
						}
						else
						{
							var storedFact = await _memoryFactStore.SupersedeAsync(targetBlock, supersedeId.Value, fact, Chat.ChatId, messageId, importance, cancellationToken);
							result.StatusIcon = MaterialIconKind.DatabaseEdit;
							if (supersedeId.Value == highestScore.Id)
								result.ResultContent = $"Fact stored with supersede successfully. ID: {storedFact.Id}, supersed fact [id: {highestScore.Id}]: {highestScore.Text}";
							else
								result.ResultContent = $"Fact stored with supersede successfully. ID: {storedFact.Id}";
							result.CompleteWithSuccess();
							return;
						}
					}
				}
				else
				{
					var storedFact = await _memoryFactStore.StoreAsync(targetBlock, fact, Chat.ChatId, messageId, importance, cancellationToken);
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
			[Description("""
				The queries to retrieve by in user's language.
				Use multiple HyDE (hypotetical) queries to greatly improve semantic matching.
				Avoid direct questions (such as "What user's preferences about food?"),
				use hypothetical answers instead (e.g. "The user's food preferences is vegetarian or meat eater.")
				or keywords (e.g. "food preferences" or "dietary restrictions")
				""")] string[] queries,
			ReactiveToolResult result,
			ToolExecutionContext ctx,
			[Description("The minimum importance score of facts to return, from 0.0 (any importance) to 1.0 (only the most important)")] double minImportance = 0.0,
			CancellationToken cancellationToken = default)
		{
			result.StatusIcon = MaterialIconKind.DatabaseSearch;

			if (queries.Length == 0)
			{
				result.ResultContent = "No queries blocks to search by.";
				result.CompleteWithError();
				return;
			}

			result.StatusTitle = $"**{queries[0]}**";

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
					var queryBatches = await Task.WhenAll(queries.Select(query =>
					{
						return _memoryFactStore.SearchAsync(block, query, minImportance: minImportance, maxCount: 10, cancellationToken: cancellationToken);
					}));
					return (block, MergeResults(queryBatches));
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

		private async Task ForgetAsync(
			[Description("The memory block that contains the fact")] string block,
			[Description("The ID of the fact to forget")] int factId,
			ReactiveToolResult result,
			ToolExecutionContext ctx,
			[Description("The deletion mode: 'soft' (default) marks the fact as Deleted and keeps it restorable; 'hard' removes the fact permanently.")] string mode = "soft",
			CancellationToken cancellationToken = default)
		{
			result.StatusIcon = MaterialIconKind.DatabaseRemove;
			result.StatusTitle = $"**{block}** [id: {factId}]";

			var targetBlock = GetBlocks(ctx, [block], requireReading: false, requireWriting: true, requireFacts: true).FirstOrDefault();
			if (targetBlock is null)
			{
				result.ResultContent = $"Memory block '{block}' is not found or does not allow writing.";
				result.CompleteWithError();
				return;
			}

			try
			{
				bool hardDelete = string.Equals(mode, "hard", StringComparison.OrdinalIgnoreCase);

				if (hardDelete)
					await _memoryFactStore.HardDeleteAsync(targetBlock, factId, cancellationToken);
				else
					await _memoryFactStore.SoftDeleteAsync(targetBlock, factId, cancellationToken);

				result.StatusIcon = MaterialIconKind.DatabaseCheck;
				result.ResultContent = hardDelete
					? $"Fact [id: {factId}] permanently deleted from memory block '{block}'."
					: $"Fact [id: {factId}] forgotten in memory block '{block}'. It can be restored later.";
				result.CompleteWithSuccess();
			}
			catch (Exception ex)
			{
				result.StatusIcon = MaterialIconKind.DatabaseOff;
				result.ResultContent = $"Failed to forget fact [id: {factId}] in memory block '{block}'. Error: {ex.Message}";
				result.CompleteWithError();
			}
		}

		private static List<MemoryFactResult> MergeResults(MemoryFactResult[][] queryBatches)
		{
			var merged = new Dictionary<int, MemoryFactResult>();
			foreach (var fact in queryBatches.SelectMany(b => b))
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
