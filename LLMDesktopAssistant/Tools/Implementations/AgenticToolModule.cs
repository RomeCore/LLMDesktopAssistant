using System.Buffers.Text;
using System.ComponentModel;
using System.Text;
using AngleSharp.Io;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Agents.Tasks;
using LLMDesktopAssistant.Attachments;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Agents;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Providers;
using LLMDesktopAssistant.Services.Instances;
using LLTSharp;
using Material.Icons;
using ModelContextProtocol.Protocol;
using RCLargeLanguageModels;
using RCLargeLanguageModels.Messages;
using SixLabors.ImageSharp;

namespace LLMDesktopAssistant.Tools.Implementations
{
	[ToolModule]
	public class AgenticToolModule : ToolModule
	{
		private readonly Chat _chat;
		private readonly TemplateLibrary _templateLibrary;
		private readonly WorkingDirectoryAccessService _fileAccess;
		private readonly IAgentManagementService _agentManager;
		private readonly IAgentTaskExecutor _agentTaskExecutor;
		private readonly IToolsetBuildingService _toolsetBuildingService;
		private readonly IModelManager _modelManager;

		public AgenticToolModule(Chat chat, TemplateLibrary templateLibrary, WorkingDirectoryAccessService fileAccess,
			IAgentManagementService agentManager, IAgentTaskExecutor agentTaskExecutor,
			IToolsetBuildingService toolsetBuildingService, IModelManager modelManager)
		{
			_chat = chat;
			_templateLibrary = templateLibrary;
			_fileAccess = fileAccess;
			_agentManager = agentManager;
			_agentTaskExecutor = agentTaskExecutor;
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

		public Task<ReactiveToolResult> AskQuestion(
			[Description("The question to ask")] string question,
			[Description("A list of tool names that can be used to answer the question.")]
			string[] allowedTools,
			ToolExecutionContext ctx,
			CancellationToken cancellationToken = default)
		{
			var systemPrompt = $"You are an agent designed to answer questions using tools.";
			return CallAgent(systemPrompt, question, allowedTools, ctx, cancellationToken);
		}

		public async Task<ReactiveToolResult> CallAgent(
			[Description("The system prompt to use in the agent's context")] string systemPrompt,
			[Description("The user message to send to the agent")] string userMessage,
			[Description("A list of tool names that can be used to answer the question.")]
			string[] allowedTools,
			ToolExecutionContext ctx,
			CancellationToken cancellationToken = default)
		{
			var modelName = _chat.Settings.Models.AgenticToolsModel;
			if (string.IsNullOrEmpty(modelName))
				return new ReactiveToolResult
				{
					ResultContent = "No agentic model selected. Say user to select an agentic model first."
				}.CompleteWithError();

			LLModel llm;
			try
			{
				llm = _modelManager.GetModel(modelName);
			}
			catch (Exception ex)
			{
				return new ReactiveToolResult
				{
					ResultContent = $"Agentic model '{modelName}' is not available: {ex.Message}"
				}.CompleteWithError();
			}

			var agentToolSettings = _agentManager.TryGetAgentDescriptor(ctx.Message.SenderAgentId)?.Tools;
			var toolMap = _toolsetBuildingService.BuildTools(ctx.Message.SenderAgentId).ToDictionary(t => t.Tool.Name);
			var tools = ImmutableList.CreateBuilder<AgentTool>();
			var errorSb = new StringBuilder();

			foreach (var allowedTool in allowedTools.Distinct())
			{
				if (toolMap.TryGetValue(allowedTool, out var toolInfo))
				{
					tools.Add(new ChatAgentTool
					{
						ChatToolInfo = toolInfo,
						ApprovalLevel = ToolApprovalLevel.AlwaysApprove,
					});
				}
				else
				{
					errorSb.AppendLine("Tool was not found: " + allowedTool);
				}
			}

			if (errorSb.Length > 0)
			{
				errorSb.Append("Valid tool names: " + string.Join(", ", toolMap.Keys));
				return new ReactiveToolResult
				{
					ResultContent = errorSb.ToString()
				}.CompleteWithError();
			}

			var agentTask = _agentTaskExecutor.Execute(new AgentTaskLaunchParameters
			{
				TaskName = "AskQuestion",
				TriggeredChat = ctx.Chat,
				TriggeredMessage = ctx.Message,
				Model = llm,
				Tools = tools.ToImmutableList(),
				InitialMessages = [
					new AgentSystemMessage { Content = systemPrompt },
					new AgentUserMessage { Content = userMessage }
				],
				AutoApproveBehaviours = agentToolSettings?.AutoApproveBehaviours ?? ToolBehaviour.None,
				DisallowedBehaviours = agentToolSettings?.DisallowedBehaviours ?? ToolBehaviour.None
			}, cancellationToken);

			try
			{
				await agentTask;

				return new ReactiveToolResult
				{
					ResultContent = $"Agent responded with: " + agentTask.LastGeneratedContent
				}.CompleteWithSuccess();
			}
			catch (Exception ex)
			{
				return new ReactiveToolResult
				{
					ResultContent = "An error occurred while calling the agent: " + ex.Message
				}.CompleteWithError();
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
			fullPath = _fileAccess.CheckedAccessPath(path, DirectoryAccessMode.Read, out var isAccessed);

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
			ToolExecutionContext ctx,
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
				fullPath ??= _fileAccess.AccessPath(path, DirectoryAccessMode.Read);
				var image = Image.Load(fullPath);
				using var memstream = new MemoryStream();
				image.SaveAsPng(memstream);
				var format = "png";
				var base64 = Convert.ToBase64String(memstream.ToArray());
				var url = "data:image/" + format + ";base64," + base64;

				var agentTask = _agentTaskExecutor.Execute(new AgentTaskLaunchParameters
				{
					TaskName = "AskQuestion",
					TriggeredChat = ctx.Chat,
					TriggeredMessage = ctx.Message,
					Model = llm,
					InitialMessages = [
						new AgentSystemMessage { Content = _templateLibrary.Retrieve("image_describer_prompt").Render().ToString()! },
						new AgentUserMessage {
							Content = "Please describe the image.",
							Attachments = [new AgentAttachment
							{
								Type = AgentAttachmentType.Image,
								Url = url
							}]
						}
					]
				}, cancellationToken);

				result.ResultContent = agentTask.LastGeneratedContent ?? string.Empty;

				int tokenCounter = 0;
				void LastResponseChanged(object? sender, PropertyChangedEventArgs e)
				{
					result.ResultContent = agentTask.LastGeneratedContent ?? string.Empty;
					tokenCounter++;

					if (tokenCounter > 1)
						result.StatusTitle = string.Format(LocalizationManager.LocalizeStatic("image_describer_status"), tokenCounter);
				}
				agentTask.PropertyChanged += LastResponseChanged;

				try
				{
					await agentTask;
				}
				finally
				{
					agentTask.PropertyChanged -= LastResponseChanged;
				}

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
