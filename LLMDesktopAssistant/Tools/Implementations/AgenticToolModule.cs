using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.Attachments;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Tools;
using LLTSharp;
using RCLargeLanguageModels;
using RCLargeLanguageModels.Agents;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Messages.Attachments;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.Tools.Implementations
{
	[ToolModule]
	public class AgenticToolModule : ToolModule
	{
		private readonly Chat _chat;
		private readonly TemplateLibrary _templateLibrary;
		private readonly IToolsetBuildingService _toolsetBuildingService;

		public AgenticToolModule(Chat chat, TemplateLibrary templateLibrary, IToolsetBuildingService toolsetBuildingService)
		{
			_chat = chat;
			_templateLibrary = templateLibrary;
			_toolsetBuildingService = toolsetBuildingService;

			AddTool(AskQuestionAsync,
				new ToolInitializationInfo
				{
					Name = "agent-ask_question",
					Description = "Asks a question using another LLM agent. This tool is useful in general chats between LLM and user, to prevent storing excessive tool calls and token consumption in main user chat.",
					Category = "agents",
					AskForConfirmation = true
				});

			AddTool(CallAgentAsync,
				new ToolInitializationInfo
				{
					Name = "agent-call",
					Description = "Calls another LLM agent with provided system message and user message with set of allowed tools.",
					Category = "agents",
					AskForConfirmation = true
				});

			AddTool(DescribeImageAsync,
				new ToolInitializationInfo
				{
					Name = "agent-describe_image",
					Description = "Describes an image using another LLM agent.",
					Category = "agents",
					AskForConfirmation = false
				});
		}

		private string ResolvePath(string path)
		{
			var baseDir = Path.GetFullPath(_chat.Settings.Environment.GetWorkingDirectory());
			if (string.IsNullOrWhiteSpace(path) || path == ".")
				return baseDir;

			var fullPath = Path.GetFullPath(Path.Combine(baseDir, path));

			if (!fullPath.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
				throw new AccessViolationException("Access outside working directory is not allowed.");

			return fullPath;
		}

		public Task<ToolResult> AskQuestionAsync(
			[Description("The question to ask")] string question,
			[Description("A list of tool names that can be used to answer the question.")]
			string[] allowedTools,
			ToolExecutionContext ctx,
			CancellationToken cancellationToken = default)
		{
			var systemPrompt = $"You are an agent designed to answer questions using tools.";
			return CallAgentAsync(systemPrompt, question, allowedTools, ctx, cancellationToken);
		}

		public async Task<ToolResult> CallAgentAsync(
			[Description("The system prompt to use in the agent's context")] string systemPrompt,
			[Description("The user message to send to the agent")] string userMessage,
			[Description("A list of tool names that can be used to answer the question.")]
			string[] allowedTools,
			ToolExecutionContext ctx,
			CancellationToken cancellationToken = default)
		{
			if (_chat.Settings.Models.AgenticToolsModel.Current is not LLModelDescriptor modelDescriptor)
				return new ToolResult(ToolResultStatus.Error, "No agentic model selected. Say user to select an agentic model first.");

			var llm = new LLModel(modelDescriptor);
			// Pass empty agent ID for the toolset builder to use all available tools (not filtered by agent)
			var toolMap = _toolsetBuildingService.BuildTools(Guid.Empty).ToDictionary(t => t.Tool.Name);
			var tools = new ToolSet();
			var errorSb = new StringBuilder();

			foreach (var allowedTool in allowedTools.Distinct())
			{
				if (toolMap.TryGetValue(allowedTool, out var toolInfo))
				{
					tools.Add(toolInfo.GetExecutableTool(ctx));
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

		public async Task<ReactiveToolResult> DescribeImageAsync(
			[Description("The path to the image file to describe")] string path,
			CancellationToken cancellationToken = default)
		{
			if (_chat.Settings.Models.VisionModel.Current is not LLModelDescriptor modelDescriptor)
				return ReactiveToolResult.CreateError("No vision model selected. Say user to select a vision model first.");

			try
			{
				var llm = new LLModel(modelDescriptor);
				path = ResolvePath(path);
				var attachment = new ImageAttachment(path);
				var result = new ReactiveToolResult();

				var messages = new List<IMessage>
				{
					new SystemMessage(_templateLibrary.Retrieve("image_describer_prompt").Render().ToString()!),
					new RCLargeLanguageModels.Messages.UserMessage(Senders.User, "Please describe the image.",
						[ attachment ])
				};

				_ = Task.Run(async () =>
				{
					try
					{
						var response = await llm.ChatStreamingAsync(messages, cancellationToken: cancellationToken);

						result.UseMarkdown = true;
						result.ResultContent = response.Content;
						result.StatusIcon = Material.Icons.MaterialIconKind.Image;

						int tokenCounter = 0;
						void Message_PartAdded(object? sender, AssistantMessageDelta e)
						{
							result.ResultContent = response.Content;
							tokenCounter++;

							if (tokenCounter > 1)
								result.StatusTitle = string.Format(LocalizationManager.LocalizeStatic("image_describer_status"), tokenCounter);
						}
						response.Message.PartAdded += Message_PartAdded;

						await response;
						response.Message.PartAdded -= Message_PartAdded;

						result.StatusTitle = null;
						result.CompleteWithSuccess();
					}
					catch (Exception ex)
					{
						result.ResultContentLines.Add($"Got error: {ex.Message}. " +
							$"May be the model is not a vision model or API is down. Please try again later.");
						result.CompleteWithError();
					}
				}, CancellationToken.None);

				return result;
			}
			catch (Exception ex)
			{
				return ReactiveToolResult.CreateError($"Error during agentic image description: {ex.Message}");
			}
		}
	}
}
