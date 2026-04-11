using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.Core.LLM;
using LLMDesktopAssistant.Core.LLM.Domain;
using LLMDesktopAssistant.Core.Modules;
using LLMDesktopAssistant.Core.ToolModules;
using RCLargeLanguageModels;
using RCLargeLanguageModels.Agents;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.Core.LLM.Services.Tools
{
	public class AgenticToolModule : ToolModule
	{
		private readonly Chat _chat;
		private readonly IToolsetBuildingService _toolsetBuildingService;

		public AgenticToolModule(Chat chat, IToolsetBuildingService toolsetBuildingService)
		{
			_chat = chat;
			_toolsetBuildingService = toolsetBuildingService;

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(AskQuestionAsync, "agent-ask_question", "Asks a question using another LLM agent. This tool is useful in general chats between LLM and user, to prevent storing excessive tool calls and token consumption in main user chat."),
				Category = "agents",
				AskForConfirmation = true
			});
			
			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(CallAgentAsync, "agent-call", "Calls another LLM agent with provided system message and user message with set of allowed tools."),
				Category = "agents",
				AskForConfirmation = true
			});
		}

		public Task<ToolResult> AskQuestionAsync(
			[Description("The question to ask")] string question,
			[Description("A list of tool names that can be used to answer the question.")]
			string[] allowedTools,
			CancellationToken cancellationToken = default)
		{
			var systemPrompt = $"You are an agent designed to answer questions using tools.";
			return CallAgentAsync(systemPrompt, question, allowedTools, cancellationToken);
		}

		public async Task<ToolResult> CallAgentAsync(
			[Description("The system prompt to use in the agent's context")] string systemPrompt,
			[Description("The user message to send to the agent")] string userMessage,
			[Description("A list of tool names that can be used to answer the question.")]
			string[] allowedTools,
			CancellationToken cancellationToken = default)
		{
			if (_chat.Settings.AgenticModel.Current is not LLModelDescriptor modelDescriptor)
				return new ToolResult(ToolResultStatus.Error, "No agentic model selected. Say user to select an agentic model first.");

			var llm = new LLModel(modelDescriptor);
			var toolMap = _toolsetBuildingService.BuildTools().ToDictionary(t => t.Tool.Name);
			var tools = new ToolSet();
			var errorSb = new StringBuilder();

			foreach (var allowedTool in allowedTools.Distinct())
			{
				if (toolMap.TryGetValue(allowedTool, out var toolInfo))
				{
					tools.Add(toolInfo.Tool);
				}
				else
				{
					errorSb.AppendLine("Invalid tool name: " + allowedTool);
				}
			}

			if (errorSb.Length > 0)
			{
				errorSb.Append("Valid tool names: " + string.Join(", ", toolMap.Keys));
				return new ToolResult(ToolResultStatus.Error, errorSb.ToString());
			}

			llm = llm.WithTools(tools);
			var executor = new LLMToolExecutor
			{
				LLMProvider = llm,
				Memory = new SlidingChatMemory
				{
					ReturnLastNMessages = 20,
					SystemInstructions = systemPrompt
				}
			};

			try
			{
				var responseMessage = await executor.GenerateResponseAsync(
					new RCLargeLanguageModels.Messages.UserMessage(userMessage), cancellationToken);

				return new ToolResult(ToolResultStatus.Success, $"Agent responded with: {responseMessage.Content}.");
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Got error: {ex.Message}");
			}
		}
	}
}