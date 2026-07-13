using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Providers;
using LLMDesktopAssistant.Utils;
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
		private readonly Random _random = new();

		private int _maxVisibleRounds = 1;
		/// <summary>
		/// Maximum number of rounds that is visible to router agent.
		/// </summary>
		public int MaxVisibleRounds
		{
			get => _maxVisibleRounds;
			set => SetProperty(ref _maxVisibleRounds, value);
		}

		private string? _additionalRouterPrompt = null;
		/// <summary>
		/// Additional prompt to be added to system prompt for router agent.
		/// </summary>
		public string? AdditionalRouterPrompt
		{
			get => _additionalRouterPrompt;
			set => SetProperty(ref _additionalRouterPrompt, value);
		}

		private bool _enforceRouterSelection = false;
		/// <summary>
		/// Whether to enforce router selection for the next agent. If disabled, router can choose none.
		/// If enabled and router not chooses anything, a random weighted agent will be selected.
		/// </summary>
		public bool EnforceRouterSelection
		{
			get => _enforceRouterSelection;
			set => SetProperty(ref _enforceRouterSelection, value);
		}

		protected override async Task<Guid?> SelectNextAgentAsync(List<ChatAgentInstance> selectFrom,
			AgentPreExecutionContext context, CancellationToken cancellationToken = default)
		{
			if (await base.SelectNextAgentAsync(selectFrom, context, cancellationToken) is Guid nextAgent)
				return nextAgent;

			var routerModelName = context.Chat.Settings.Models.AgenticRouterModel;
			if (string.IsNullOrEmpty(routerModelName))
				throw new InvalidOperationException("Agentic router model is not selected. Please select a router model in chat settings.");

			var modelManager = context.Services.GetRequiredService<IModelManager>();
			LLModel llm;
			try
			{
				llm = modelManager.GetModel(routerModelName);
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException($"Agentic router model '{routerModelName}' is not available: {ex.Message}");
			}

			var templateLibrary = context.Services.GetRequiredService<TemplateLibrary>();

			var agents = selectFrom
				.Select(a => context.AgentManager.GetAgentDescriptor(a.AgentId))
				.ToList();

			if (agents.Count == 0)
				return null;

			var agentNames = string.Join("\n", agents.Select(a => a.Info.Name));

			var template = (IMessagesTemplate)templateLibrary.Retrieve("router_prompt");
			var messages = template.RenderRCLLM(new
			{
				agents = agents.Select(a => new
				{
					name = a.Info.Name,
					description = a.Info.Description
				}).ToArray(),
				enforce_selection = EnforceRouterSelection,
				additional_prompt = AdditionalRouterPrompt,
				context = SelectContext(context),
			}).ToList();

			var response = await llm.ChatAsync(messages, cancellationToken: cancellationToken);
			var content = response.Message.Content?.TrimStart('@');
			var selectedAgent = agents.FirstOrDefault(a => a.Info.Name == content)?.Id;

			Log.Information("Router model selected next agent: {Content}", content);

			if (selectedAgent == null && EnforceRouterSelection)
			{
				Log.Information("Picking a random agent...");

				// Pick a random agent, if router did not choose anything
				double weightSum = selectFrom.Sum(a => a.Weight);
				double randomValue = _random.NextDouble() * weightSum;
				double currentWeight = 0;

				foreach (var agent in selectFrom)
				{
					currentWeight += agent.Weight;
					if (currentWeight >= randomValue)
						return agent.AgentId;
				}
			}

			return selectedAgent;
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
