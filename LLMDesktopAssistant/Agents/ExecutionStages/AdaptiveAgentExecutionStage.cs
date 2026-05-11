using LLMDesktopAssistant.LLM.Services;
using LLTSharp;
using RCLargeLanguageModels;
using RCLargeLanguageModels.Messages;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Agents.ExecutionStages
{
	public class AdaptiveAgentExecutionStage : MentionableAgentExecutionStage
	{
		private int _maxVisibleRounds = 1;
		/// <summary>
		/// Maximum number of rounds that is visible to router agent.
		/// </summary>
		public int MaxVisibleRounds
		{
			get => _maxVisibleRounds;
			set => SetProperty(ref _maxVisibleRounds, value);
		}

		protected override async Task<Guid?> SelectNextAgentAsync(List<AgentInstance> selectFrom,
			AgentPreExecutionContext context, CancellationToken cancellationToken = default)
		{
			if (await base.SelectNextAgentAsync(selectFrom, context, cancellationToken) is Guid nextAgent)
				return nextAgent;

			var model = context.Chat.Settings.Models.AgenticRouterModel;
			if (!model.Available)
				throw new InvalidOperationException("Agentic router model is not available.");

			var llm = model.Model!;
			var templateLibrary = context.Services.GetRequiredService<TemplateLibrary>();

			var agents = selectFrom
				.Select(a => context.AgentManager.GetAgentDescriptor(a.AgentId))
				.ToList();

			if (agents.Count == 0)
				return null;

			var agentNames = string.Join("\n", agents.Select(a => a.Info.Name));

			var template = (IMessagesTemplate)templateLibrary.Retrieve("router_prompt");
			var rendered = template.Render(new
			{
				context = SelectContext(context),
				agents = agents.Select(a => new
				{
					name = a.Info.Name,
					description = a.Info.Description
				}).ToArray()
			});
			var messages = rendered.Select(m => m.Role switch
			{
				LLTSharp.Role.System => (IMessage)new SystemMessage(m.Content),
				LLTSharp.Role.User => new UserMessage(m.Content),
				_ => throw new ArgumentOutOfRangeException(nameof(m.Role), $"Unknown role: {m.Role}")
			}).ToList();

			var response = await llm.ChatAsync(messages, cancellationToken: cancellationToken);
			var content = response.Message.Content?.TrimStart('@');
			Log.Information("Router model selected next agent: {Content}", content);
			return agents.FirstOrDefault(a => a.Info.Name == content)?.Id;
		}

		private string? SelectContext(AgentPreExecutionContext context)
		{
			var rounds = MessagesInterface.GroupMessagesIntoRounds(context.Chat.Messages, MaxVisibleRounds);
			var promptBuilder = context.Services.GetRequiredService<IPromptChatBuilder>();

			var sb = new StringBuilder();

			foreach (var round in rounds)
			{
				foreach (var message in round)
				{
					sb.AppendLine(promptBuilder.RenderMessage(message.Message));
				}
			}

			return sb.ToString();
		}
	}
}