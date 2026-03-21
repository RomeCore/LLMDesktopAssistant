using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.LLM;
using LLMDesktopAssistant.Modules;
using RCLargeLanguageModels.Agents;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.ToolModules
{
	public class AgenticToolModule : ToolModule
	{
		public AgenticToolModule()
		{
			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(AskQuestionAsync, "agent-ask_question", "Asks a question using another LLM agent. This tool is useful in general chats between LLM and user, to prevent storing excessive tool calls and token consumption in main user chat."),
				AskForConfirmation = true
			});
			
			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(CallAgentAsync, "agent-call", "Calls another LLM agent with provided system message and user message with set of allowed tools."),
				AskForConfirmation = true
			});
		}

		public Task<ToolResult> AskQuestionAsync(
			[Description("The question to ask")] string question,
			[Description(
				"Optional: A list of tool names that can be used to answer the question. " +
				"If not provided, default set of tools will be used.")]
			string[]? allowedTools = null,
			CancellationToken cancellationToken = default)
		{
			var systemPrompt = $"You are an agent designed to answer questions using tools.";
			return CallAgentAsync(systemPrompt, question, allowedTools, cancellationToken);
		}

		public async Task<ToolResult> CallAgentAsync(
			[Description("The system prompt to use in the agent's context")] string systemPrompt,
			[Description("The user message to send to the agent")] string userMessage,
			[Description(
				"Optional: A list of tool names that can be used to answer the question. " +
				"If not provided, default set of tools will be used.")]
			string[]? allowedTools = null,
			CancellationToken cancellationToken = default)
		{
			var toolInfos = ModuleManager.GetAll<ToolModule>()
				.Where(t => t.Enabled)
				.SelectMany(t => t.GetTools())
				.ToList();

			var llm = ModuleManager.GetDynamic<ILLMProvider>().GetLLM();
			var toolMap = toolInfos.ToDictionary(t => t.Tool.Name);
			var tools = new ToolSet();
			var errorSb = new StringBuilder();

			if (allowedTools != null)
			{
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
			}
			else
			{
				foreach (var allowedTool in new string[] {
					"calculation-calculate",
					"general-generateGUID",
					"general-generateRandomInteger",
					"general-GenerateRandomFloat",
					"web-get",
					"web-post",
					"web-status",
					"web-get_html",
					"web-parse",
					"web-search"
				})
				{
					if (toolMap.TryGetValue(allowedTool, out var toolInfo))
					{
						tools.Add(toolInfo.Tool);
					}
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
				var responseMessage = await executor.GenerateResponseAsync(new UserMessage(userMessage), cancellationToken);

				return new ToolResult(ToolResultStatus.Success, $"Agent responded with: {responseMessage.Content}.");
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Got error: {ex.Message}");
			}
		}
	}
}