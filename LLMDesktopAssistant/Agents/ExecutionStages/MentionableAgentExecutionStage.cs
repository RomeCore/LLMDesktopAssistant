using System.Text.RegularExpressions;

namespace LLMDesktopAssistant.Agents.ExecutionStages
{
	public abstract class MentionableAgentExecutionStage : RepeatableAgentExecutionStage
	{
		private bool _enableMentions = true;
		public bool EnableMentions
		{
			get => _enableMentions;
			set => SetProperty(ref _enableMentions, value);
		}

		protected virtual Task<Guid?> DetectMentionAsync(List<ChatAgentInstance> selectFrom,
			AgentPreExecutionContext context, CancellationToken cancellationToken = default)
		{
			if (context.PreviousAgentExecuted.HasValue && 
				!context.AgentManager.GetAgentDescriptor(context.PreviousAgentExecuted.Value).ExecutionConditions.CanMentionOthers)
				return Task.FromResult<Guid?>(null);

			var strToSearch = context.Chat.Messages.LastOrDefault()?.Message.Content;

			if (string.IsNullOrEmpty(strToSearch))
				return Task.FromResult<Guid?>(null);

			var agents = selectFrom
				.Select(a => context.AgentManager.GetAgentDescriptor(a.AgentId))
				.Where(a => a.ExecutionConditions.CanBeMentioned)
				.ToList();

			if (agents.Count == 0)
				return Task.FromResult<Guid?>(null);

			var agentNames = agents
				.Select(a => a.Info.Name)
				.ToList();

			var regex = new Regex(string.Join("|", agentNames.Select(a => $"@{Regex.Escape(a)}\\b")), RegexOptions.Compiled);
			var firstMatch = regex.Match(strToSearch);

			if (!firstMatch.Success)
				return Task.FromResult<Guid?>(null);

			var agentName = firstMatch.Value[1..];
			var agentId = agents.FirstOrDefault(a => a.Info.Name == agentName)?.Id;
			return Task.FromResult(agentId);
		}

		protected override async Task<Guid?> SelectNextAgentAsync(List<ChatAgentInstance> selectFrom,
			AgentPreExecutionContext context, CancellationToken cancellationToken = default)
		{
			if (EnableMentions)
			{
				var detectedMention = await DetectMentionAsync(selectFrom, context, cancellationToken);

				if (detectedMention.HasValue)
					return detectedMention.Value;
			}
			return null;
		}
	}
}