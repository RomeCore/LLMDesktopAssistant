using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.LLM.Conversations;
using LLMDesktopAssistant.Modules;
using LLMDesktopAssistant.MVVM;
using LLMDesktopAssistant.ToolModules;
using Microsoft.Extensions.AI;
using RCLargeLanguageModels;
using RCLargeLanguageModels.Agents;
using RCLargeLanguageModels.Clients.Ollama;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Tasks;
using RCLargeLanguageModels.Tools;
using Serilog;

namespace LLMDesktopAssistant.LLM.MVVM
{
	public class ChatToolExecutor : LLMToolExecutor
	{
		private readonly ChatViewModel _viewModel;
		private readonly ChatLLMInfo _llmInfo;
		
		public ChatToolExecutor(ChatViewModel vm)
		{
			_viewModel = vm;
			_llmInfo = _viewModel.GetConfiguredLLM();

			LLMProvider = _llmInfo.LLM;
			Memory = _viewModel.ConversationManager.CreateMemory(Memory as SummarizingChatMemory);

			var scm = (SummarizingChatMemory)Memory;
			scm.Summarizer ??= new StatelessAgent
			{
				SystemInstructions = """
					You are a conversation memory summarizer used inside an AI system.

					Your task is to compress a conversation into a concise but information-dense summary that will be used as long-term memory for future interactions.

					CRITICAL REQUIREMENTS:

					1. Preserve all important information:
					   - User goals, intents, and requests
					   - Key facts and constraints
					   - Decisions that were made
					   - Unresolved questions or tasks
					   - Important context needed for future responses

					2. Preserve technical details when present:
					   - Code logic, architecture decisions, APIs
					   - Error messages and their causes
					   - Tool usage results and outcomes

					3. Maintain continuity:
					   - If a previous summary is provided, integrate it with new information
					   - Do NOT repeat information unnecessarily
					   - Do NOT contradict earlier context

					4. Remove unimportant content:
					   - Small talk
					   - Repetitions
					   - Irrelevant details

					5. Be structured and compact:
					   - Use clear sections if helpful
					   - Prefer bullet points for dense information
					   - Avoid long prose

					6. Be precise:
					   - Do not generalize important details
					   - Do not invent information
					   - Do not omit critical steps in reasoning or workflow

					7. Tool usage handling:
					   - If tools were used, include:
					     - What tool was used
					     - Why it was used
					     - The result of the tool

					OUTPUT FORMAT:

					Return only the summary text.

					The summary should be compact but complete enough so that another AI can continue the conversation without needing the original messages.
					""",
				LLMProvider = _llmInfo.LLM
			};
		}

		public void OnTurnEnded()
		{
			_viewModel.ConversationManager.ImportSummaryFromMemory((SummarizingChatMemory)Memory!);
		}

		protected override void OnMessageReceived(IMessage message)
		{
			base.OnMessageReceived(message);

			_viewModel.ConversationManager.AppendMessage(message);
		}

		protected override async Task<ToolResult?> OnToolExecutionBegin(ITool tool, IToolCall toolCall, CancellationToken cancellationToken)
		{
			if (_llmInfo.ToolInfos.TryGetValue(tool.Name, out var toolInfo))
			{
				if (toolInfo.AskForConfirmation)
				{
					if (await _viewModel.MessageSequence.AskToolExecuteAsync(toolCall, cancellationToken))
						return await base.OnToolExecutionBegin(tool, toolCall, cancellationToken);
					else
						return new ToolResult(ToolResultStatus.Cancelled, "User cancelled the tool execution.");
				}
			}

			return await base.OnToolExecutionBegin(tool, toolCall, cancellationToken);
		}

		protected override async Task<ToolResult> OnToolExecutionEnd(ITool tool, IToolCall toolCall, Task<ToolResult> resultTask, CancellationToken cancellationToken)
		{
			if (resultTask.IsFaulted)
				return new ToolResult(ToolResultStatus.Error, resultTask.Exception?.InnerException?.Message ?? "An unknown error occurred.");
			else if (resultTask.IsCanceled)
				return new ToolResult(ToolResultStatus.Cancelled, "The tool execution was cancelled.");
			else if (resultTask.IsCompletedSuccessfully)
				return resultTask.Result;

			return await base.OnToolExecutionEnd(tool, toolCall, resultTask, cancellationToken);
		}
	}

	/// <summary>
	/// Manages the state and behavior of a LLM in the chat session.
	/// </summary>
	public class ChatLLMInfo
	{
		/// <summary>
		/// The large language model used for the chat session.
		/// </summary>
		public required LLModel LLM { get; init; }

		/// <summary>
		/// The tools available for use in the chat session.
		/// </summary>
		public required ImmutableDictionary<string, ToolInfo> ToolInfos { get; init; }
	}

	[ViewModelFor(typeof(ChatView))]
	public class ChatViewModel : ViewModelBase
	{
		private CancellationTokenSource? _sendCts;

		private string _systemPrompt = "Ты виртуальный ассистент в чате с пользователем. " +
			"Используй инструмент 'agent-ask_question', чтобы не засорять чат лишними вызовами инструментов при ответах на вопросы, на которые ты не знаешь ответов.";
		/// <summary>
		/// Gets or sets the system prompt that will be used as a starting point for conversation.
		/// </summary>
		public string SystemPrompt
		{
			get => _systemPrompt;
			set => SetProperty(ref _systemPrompt, value);
		}

		private ObservableCollection<ToolModule> _additionalTools = [];
		/// <summary>
		/// Gets or sets the collection of additional tool modules.
		/// </summary>
		public ObservableCollection<ToolModule> AdditionalTools
		{
			get => _additionalTools;
			set => SetProperty(ref _additionalTools, value);
		}

		/// <summary>
		/// Gets the message sequence that represents the conversation history.
		/// </summary>
		public MessageSequenceViewModel MessageSequence { get; }

		/// <summary>
		/// Gets the conversation manager that manages the current conversation.
		/// </summary>
		public ConversationManager ConversationManager { get; }

		private string _userInput = string.Empty;
		/// <summary>
		/// Gets or sets the user input to be sent in the next conversation turn.
		/// </summary>
		public string UserInput
		{
			get => _userInput;
			set => SetProperty(ref _userInput, value);
		}

		/// <summary>
		/// Command to send a message. This command is bound to the UI and triggers the SendMessage method.
		/// </summary>
		public ICommand SendMessageCommand { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ChatViewModel"/> class.
		/// </summary>
		public ChatViewModel(ConversationManager conversationManager)
		{
			SendMessageCommand = new AsyncRelayCommand(async ct =>
			{
				try
				{
					await SendMessageAsync(ct);
				}
				catch (Exception ex)
				{
					Log.Error(ex, "An error occurred while sending a message: {error}.", ex);
				}
				finally
				{
					// TODO
					// Turns.Last().State = ConversationTurnState.Complete;
				}
			});

			ConversationManager = conversationManager;
			MessageSequence = new MessageSequenceViewModel(conversationManager);

			AdditionalTools = [ new AgenticToolModule() ];
		}

		/// <summary>
		/// Returns the configured LLM model. This method can be overridden to provide custom configuration.
		/// </summary>
		/// <returns>The configured LLM model.</returns>
		public virtual ChatLLMInfo GetConfiguredLLM()
		{
			var llm = ModuleManager.GetDynamic<ILLMProvider>().GetLLM();

			var toolInfos = ModuleManager.GetAll<ToolModule>()
				.Concat(AdditionalTools)
				.Where(t => t.Enabled)
				.SelectMany(t => t.GetTools())
				.ToList();

			var allTools = new ToolSet(toolInfos.Select(i => i.Tool));

			// Add existing LLM's tools.
			foreach (var tool in llm.Tools)
				allTools.Add(tool);
			llm = llm.WithTools(allTools);

			return new ChatLLMInfo
			{
				LLM = llm,
				ToolInfos = toolInfos.ToImmutableDictionary(k => k.Tool.Name)
			};
		}

		/// <summary>
		/// Sends a message to the LLM and updates the conversation turns.
		/// </summary>
		/// <param name="userMessage">The user message to be sent.</param>
		/// <param name="cts">The cancellation token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		public async Task SendMessageAsync(IUserMessage userMessage, CancellationToken cts = default)
		{
			try
			{
				_sendCts?.Cancel(); // Cancel any previous send operation
				_sendCts = CancellationTokenSource.CreateLinkedTokenSource(cts);
				cts = _sendCts.Token;

				var executor = new ChatToolExecutor(this);
				await executor.GenerateStreamingResponseAsync(userMessage, cts);
				executor.OnTurnEnded();
			}
			catch (Exception ex)
			{
				Log.Error(ex, "An error occurred while sending a message: {error}.", ex);
			}
		}

		/// <summary>
		/// Sends a message to the LLM and updates the conversation turns.
		/// </summary>
		/// <param name="cts">The cancellation token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		public Task SendMessageAsync(CancellationToken cts = default)
		{
			var userMessage = new UserMessage(UserInput);
			UserInput = string.Empty;
			return SendMessageAsync(userMessage, cts);
		}
	}





}