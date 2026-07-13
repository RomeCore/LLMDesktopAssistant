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
using LLMDesktopAssistant.Providers;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Tools;
using LLTSharp;
using Material.Icons;
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
		private readonly WorkingDirectoryAccessService _fileAccess;
		private readonly IToolsetBuildingService _toolsetBuildingService;
		private readonly IModelManager _modelManager;

		public AgenticToolModule(Chat chat, TemplateLibrary templateLibrary, WorkingDirectoryAccessService fileAccess,
			IToolsetBuildingService toolsetBuildingService,IModelManager modelManager)
		{
			_chat = chat;
			_templateLibrary = templateLibrary;
			_fileAccess = fileAccess;
			_toolsetBuildingService = toolsetBuildingService;
			_modelManager = modelManager;

			AddTool(AskQuestion,
				new ToolInitializationInfo
				{
					Name = "agent-ask_question",
					Description = "Asks a question using another LLM agent. This tool is useful in general chats between LLM and user, to prevent storing excessive tool calls and token consumption in main user chat.",
					Category = "agents",
					DefaultExpectedBehaviour = ToolBehaviour.AgentExecution | ToolBehaviour.LongRunningTask
				});

			AddTool(CallAgent,
				new ToolInitializationInfo
				{
					Name = "agent-call",
					Description = "Calls another LLM agent with provided system message and user message with set of allowed tools.",
					Category = "agents",
					DefaultExpectedBehaviour = ToolBehaviour.AgentExecution | ToolBehaviour.LongRunningTask
				});

			AddTool(DescribeImage, DescribeImageStreaming, DescribeImagePreview,
				new ToolInitializationInfo
				{
					Name = "agent-describe_image",
					Description = "Describes an image using another LLM agent.",
					Category = "agents",
					DefaultExpectedBehaviour = ToolBehaviour.AgentExecution | ToolBehaviour.LongRunningTask |
						ToolBehaviour.FileRead | ToolBehaviour.AccessOutsideWorkdir
				});
		}

		public Task<ToolResult> AskQuestion(
			[Description("The question to ask")] string question,
			[Description("A list of tool names that can be used to answer the question.")]
			string[] allowedTools,
			ToolExecutionContext ctx,
			CancellationToken cancellationToken = default)
		{
			var systemPrompt = $"You are an agent designed to answer questions using tools.";
			return CallAgent(systemPrompt, question, allowedTools, ctx, cancellationToken);
		}

		public async Task<ToolResult> CallAgent(
			[Description("The system prompt to use in the agent's context")] string systemPrompt,
			[Description("The user message to send to the agent")] string userMessage,
			[Description("A list of tool names that can be used to answer the question.")]
			string[] allowedTools,
			ToolExecutionContext ctx,
			CancellationToken cancellationToken = default)
		{
			var modelName = _chat.Settings.Models.AgenticToolsModel;
			if (string.IsNullOrEmpty(modelName))
				return new ToolResult(ToolResultStatus.Error, "No agentic model selected. Say user to select an agentic model first.");

			LLModel llm;
			try
			{
				llm = _modelManager.GetModel(modelName);
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Agentic model '{modelName}' is not available: {ex.Message}");
			}

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

		public StreamingToolArgumentsAnalysisResult DescribeImageStreaming(
			string? path)
		{
			path ??= "?";
			return new StreamingToolArgumentsAnalysisResult
			{
				StatusIcon = MaterialIconKind.Image,
				StatusTitle = $"**{path}**"
			};
		}

		public PreviewToolExecutionResult DescribeImagePreview(
			string path, [SharedContext] out string fullPath)
		{
			fullPath = _fileAccess.CheckedAccessPath(path, out var isAccessed);

			if (!File.Exists(fullPath))
			{
				new PreviewToolExecutionResult
				{
					StatusIcon = MaterialIconKind.Image,
					StatusTitle = $"**{path}**",
					ExpectedBehaviour = !isAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None,
					InterruptingSuccess = false,
					InterruptingContent = $"File not found: {path}"
				};
			}

			return new PreviewToolExecutionResult
			{
				StatusIcon = MaterialIconKind.Image,
				StatusTitle = $"**{path}**",
				ExpectedBehaviour = ToolBehaviour.AgentExecution | ToolBehaviour.LongRunningTask | ToolBehaviour.FileRead |
					(!isAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None)
			};
		}

		public async Task DescribeImage(
			[SharedContext] string? fullPath,
			ReactiveToolResult result,
			[Description("The path to the image file to describe")] string path,
			CancellationToken cancellationToken = default)
		{
			result.StatusIcon = MaterialIconKind.Image;
			result.StatusTitle = $"**{path}**";
			result.UseMarkdown = true;

			var modelName = _chat.Settings.Models.VisionModel;
			if (string.IsNullOrEmpty(modelName))
			{
				result.ResultContent = $"No vision model selected. Say user to select a vision model first.";
				result.CompleteWithError();
				return;
			}

			if (!File.Exists(fullPath))
			{
				result.ResultContent = $"File not found: {path}";
				result.CompleteWithError();
				return;
			}

			LLModel llm;
			try
			{
				llm = _modelManager.GetModel(modelName);
			}
			catch (Exception ex)
			{
				result.ResultContent = $"Vision model '{modelName}' is not available: {ex.Message}";
				result.CompleteWithError();
				return;
			}

			try
			{
				fullPath ??= _fileAccess.AccessPath(path);
				var attachment = new SerializableImageAttachment(path);

				var messages = new List<IMessage>
				{
					new SystemMessage(_templateLibrary.Retrieve("image_describer_prompt").Render().ToString()!),
					new RCLargeLanguageModels.Messages.UserMessage(Senders.User, "Please describe the image.",
						[ attachment ])
				};

				var response = await llm.ChatStreamingAsync(messages, cancellationToken: cancellationToken);

				result.ResultContent = response.Content;

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
		}
	}
}
