using System.ComponentModel;
using System.Text;
using LLMDesktopAssistant.Agents.Memory;
using LLMDesktopAssistant.Agents.Tasks;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.LLM.Services.Agents;
using LLMDesktopAssistant.LLM.Services.Prompting;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Prompting;
using LLMDesktopAssistant.Providers;
using Material.Icons;
using RCLargeLanguageModels;
using SixLabors.ImageSharp;

namespace LLMDesktopAssistant.Tools.Implementations
{
	[ToolModule]
	public class AgenticToolModule : ToolModule
	{
		private readonly Chat _chat;
		private readonly TemplateLibraryAccessor _templates;
		private readonly WorkingDirectoryAccessService _fileAccess;
		private readonly IAgentManagementService _agentManager;
		private readonly IAgentTaskExecutor _agentTaskExecutor;
		private readonly IToolsetBuildingService _toolsetBuildingService;
		private readonly ISkillsetBuildingService _skillsetBuildingService;
		private readonly IModelManager _modelManager;

		public AgenticToolModule(Chat chat, TemplateLibraryAccessor templates, WorkingDirectoryAccessService fileAccess,
			IAgentManagementService agentManager, IAgentTaskExecutor agentTaskExecutor,
			IToolsetBuildingService toolsetBuildingService, ISkillsetBuildingService skillsetBuildingService, IModelManager modelManager)
		{
			_chat = chat;
			_templates = templates;
			_fileAccess = fileAccess;
			_agentManager = agentManager;
			_agentTaskExecutor = agentTaskExecutor;
			_toolsetBuildingService = toolsetBuildingService;
			_skillsetBuildingService = skillsetBuildingService;
			_modelManager = modelManager;

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

		private async Task<ReactiveToolResult> CallAgent(
			[Description("The title of the agent call to be visible in UI")] string? callTitle,
			[Description("The system prompt to use in the agent's context")] string systemPrompt,
			[Description("The user message to send to the agent")] string userMessage,
			[Description("A list of tool names that can be used by agent.")] string[] allowedTools,
			[Description("A list of skill names that can be used by agent.")] string[] allowedSkills,
			ToolExecutionContext ctx,
			[Description("""
				A list of memory block names to make accessible to the agent via memory tools.
				Memory must be enabled for the chat and the calling agent.
				""")] string[]? memoryBlocks = null,
			[Description("""
				If true - waits for end of execution and returns the contents of last message.
				If false - returns agent task ID immediately, the agent will continue to run in the background.
				""")] bool wait = true,
			CancellationToken cancellationToken = default)
		{
			var modelName = _chat.Settings.Models.GetEffectiveSelection().AgenticToolsModel;
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

			var agentDescriptor = _agentManager.GetAgentDescriptor(ctx.Message.SenderAgentId);

			var errorSb = new StringBuilder();

			var tools = ImmutableList.CreateBuilder<AgentTool>();
			if (allowedTools.Length > 0)
			{
				var toolMap = _toolsetBuildingService.GetToolsForAgent(agentDescriptor).ToDictionary(t => t.Name);

				int notFound = 0;
				foreach (var allowedTool in allowedTools.Distinct())
				{
					if (toolMap.TryGetValue(allowedTool, out var toolInfo))
					{
						tools.Add(new ChatAgentTool
						{
							ChatToolInfo = toolInfo,
							ApprovalLevel = toolInfo.ApprovalLevel
						});
					}
					else
					{
						notFound++;
						errorSb.AppendLine("Tool was not found: " + allowedTool);
					}
				}

				if (notFound > 0)
					errorSb.Append("Valid tool names: " + string.Join(", ", toolMap.Keys));
			}

			var skills = ImmutableList.CreateBuilder<AgentSkill>();
			if (allowedSkills.Length > 0)
			{
				var skillMap = _skillsetBuildingService.GetSkillsForAgent(agentDescriptor).ToDictionary(s => s.Name);

				int notFound = 0;
				foreach (var allowedSkill in allowedSkills.Distinct())
				{
					if (skillMap.TryGetValue(allowedSkill, out var skillInfo))
					{
						skills.Add(new ChatAgentSkill
						{
							ChatSkillInfo = skillInfo
						});
					}
					else
					{
						notFound++;
						errorSb.AppendLine("Skill was not found: " + allowedSkill);
					}
				}

				if (notFound > 0)
					errorSb.Append("Valid skill names: " + string.Join(", ", skillMap.Keys));
			}

			ImmutableList<TaskMemoryBlock>? resolvedMemoryBlocks = null;
			if (memoryBlocks is { Length: > 0 })
			{
				var available = TaskMemoryBlock.ResolveBlocks(_chat, agentDescriptor);
				var availableMap = available.ToImmutableDictionary(b => b.Block.Name);
				var resolved = ImmutableList.CreateBuilder<TaskMemoryBlock>();

				int notFound = 0;
				foreach (var blockName in memoryBlocks.Distinct())
				{
					if (availableMap.TryGetValue(blockName, out var block))
					{
						resolved.Add(block);
					}
					else
					{
						notFound++;
						errorSb.AppendLine("Memory block was not found: " + blockName);
					}
				}

				if (notFound > 0)
					errorSb.Append("Valid memory block names: " + string.Join(", ", availableMap.Keys));

				resolvedMemoryBlocks = resolved.ToImmutable();
			}

			if (errorSb.Length > 0)
			{
				return new ReactiveToolResult
				{
					ResultContent = errorSb.ToString()
				}.CompleteWithError();
			}

			var policy = agentDescriptor.Tools.GetEffectivePolicy(ctx.Chat.Settings);
			ToolBehaviour autoApproveBehaviours = policy.AutoApproveBehaviours,
				disallowedBehaviours = policy.DisallowedBehaviours;

			try
			{
				var ct = wait ? cancellationToken : CancellationToken.None;

				var agentTask = _agentTaskExecutor.Execute(new AgentTaskLaunchParameters
				{
					TaskName = callTitle,
					TriggeredChat = ctx.Chat,
					TriggeredMessage = ctx.Message,
					Model = llm,
					Tools = tools.ToImmutableList(),
					Skills = skills.ToImmutableList(),
					MemoryBlocks = resolvedMemoryBlocks is { Count: > 0 } ? resolvedMemoryBlocks : null,
					InitialMessages = [
						new AgentSystemMessage { Content = systemPrompt },
						new AgentUserMessage { Content = userMessage }
					],
					AutoApproveBehaviours = autoApproveBehaviours,
					DisallowedBehaviours = disallowedBehaviours
				}, ct);

				if (wait)
				{
					await agentTask;

					return new ReactiveToolResult
					{
						ResultContent = string.IsNullOrWhiteSpace(agentTask.LastGeneratedContent) ?
							"Agent did not generate any content." : agentTask.LastGeneratedContent,
						UseMarkdown = true
					}.CompleteWithSuccess();
				}
				else
				{
					return new ReactiveToolResult
					{
						ResultContent = $"Agent launched with task ID {agentTask.Id}."
					}.CompleteWithSuccess();
				}
			}
			catch (Exception ex)
			{
				return new ReactiveToolResult
				{
					ResultContent = "An error occurred while calling the agent: " + ex.Message
				}.CompleteWithError();
			}
		}

		private StreamingToolArgumentsAnalysisResult DescribeImageStreaming(
			string? path)
		{
			path ??= "?";
			return new StreamingToolArgumentsAnalysisResult
			{
				StatusIcon = MaterialIconKind.Image,
				StatusTitle = $"**{path}**"
			};
		}

		private PreviewToolExecutionResult DescribeImagePreview(
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

		private async Task DescribeImage(
			[SharedContext] string? fullPath,
			ReactiveToolResult result,
			[Description("The title of the agent call to be visible in UI")] string? callTitle,
			[Description("The path to the image file to describe")] string path,
			ToolExecutionContext ctx,
			CancellationToken cancellationToken = default)
		{
			result.StatusIcon = MaterialIconKind.Image;
			result.StatusTitle = $"**{path}**";
			result.UseMarkdown = true;

			var modelName = _chat.Settings.Models.GetEffectiveSelection().VisionModel;
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
					TaskName = callTitle,
					TriggeredChat = ctx.Chat,
					TriggeredMessage = ctx.Message,
					Model = llm,
					InitialMessages = [
						new AgentSystemMessage { Content = _templates.GetTextTemplate("image_describer_prompt").Render().ToString()! },
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
