using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Modules;
using LLMDesktopAssistant.MVVM;
using LLMDesktopAssistant.Tabs;
using LLMDesktopAssistant.ToolModules;
using Microsoft.Extensions.AI;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Tasks;
using RCLargeLanguageModels.Tools;
using Serilog;

namespace LLMDesktopAssistant.LLM.MVVM
{
	[ViewModelFor(typeof(ChatView))]
	[TabTool("chat")]
	public class ChatViewModel : ViewModelBase
	{
		private CancellationTokenSource? _sendCts;

		private string _systemPrompt = "You are a helpful assistant.";
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

		private MessageSequenceViewModel _messageSequence = new();
		/// <summary>
		/// Gets or sets the message sequence that represents the conversation history.
		/// </summary>
		public MessageSequenceViewModel MessageSequence
		{
			get => _messageSequence;
			set => SetProperty(ref _messageSequence, value);
		}

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
		public ChatViewModel()
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
		}

		/// <summary>
		/// Sends a message to the LLM and updates the conversation turns.
		/// </summary>
		/// <param name="cts">The cancellation token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		public async Task SendMessageAsync(CancellationToken cts)
		{
			_sendCts?.Cancel(); // Cancel any previous send operation
			_sendCts = CancellationTokenSource.CreateLinkedTokenSource(cts);
			cts = _sendCts.Token;
			var sequence = MessageSequence;

			var llm = ModuleManager.GetDynamic<ILLMProvider>().GetLLM();

			var allTools = new ToolSet(ModuleManager.GetAll<ToolModule>()
				.Concat(AdditionalTools)
				.Where(t => t.Enabled)
				.SelectMany(t => t.GetTools()));

			// Add existing LLM's tools.
			foreach (var tool in llm.Tools)
				allTools.Add(tool);
			llm = llm.WithTools(allTools);

			var userMessage = new UserMessage(UserInput);
			var messages = new List<IMessage>();
			messages.Add(new SystemMessage(SystemPrompt));
			messages.AddRange(sequence.Messages);
			messages.Add(userMessage);

			var response = await llm.ChatStreamingAsync(messages, cts);
			var assistantMessage = response.Message;
			UserInput = string.Empty;

			sequence.Messages.Add(userMessage);
			sequence.Messages.Add(assistantMessage);
			messages.Add(assistantMessage);

			while (true)
			{
				ConcurrentDictionary<IToolCall, IToolMessage> toolMessageMap = [];
				List<Task> toolExecutionTasks = [];

				void ProcessToolCall(IToolCall toolCall)
				{
					switch (toolCall)
					{
						case FunctionToolCall functionCall:

							var tool = allTools.Get(toolCall.ToolName) as FunctionTool ??
								throw new InvalidOperationException($"FunctionTool '{functionCall.ToolName}' not found in the current toolset.");

							var executionTask = tool.ExecuteAsync(functionCall.Args, cts)
								.ContinueWith(t =>
								{
									ToolResult toolMsgContent;

									if (t.IsCanceled)
										toolMsgContent = "CANCELLED";
									else if (t.IsFaulted)
										toolMsgContent = "ERROR";
									else
										toolMsgContent = t.Result;

									var toolMessage = new ToolMessage(toolMsgContent, toolCall.Id, toolCall.ToolName);
									toolMessageMap[toolCall] = toolMessage;
								}, cts);
							toolExecutionTasks.Add(executionTask);

							break;

						default:
							throw new InvalidOperationException($"Unknown tool call type: {toolCall.GetType()}.");
					}
				}

				foreach (var toolCall in assistantMessage.ToolCalls)
				{
					ProcessToolCall(toolCall);
				}

				void PartHandler(object? s, AssistantMessageDelta e)
				{
					if (e.NewToolCalls?.Count > 0)
						foreach (var toolCall in e.NewToolCalls)
							ProcessToolCall(toolCall);
				}
				void CompletedHandler(object? s, CompletedEventArgs e)
				{
					assistantMessage.PartAdded -= PartHandler;
					assistantMessage.Completed -= CompletedHandler;
				}
				if (!assistantMessage.CompletionToken.IsCompleted)
				{
					assistantMessage.PartAdded += PartHandler;
					assistantMessage.Completed += CompletedHandler;
				}

				await assistantMessage;

				if (toolExecutionTasks.Count > 0)
				{
					await Task.WhenAll(toolExecutionTasks);

					// Send the tool results back to the LLM.

					foreach (var toolCall in assistantMessage.ToolCalls)
					{
						var toolMessage = toolMessageMap[toolCall];
						messages.Add(toolMessage);
						sequence.Messages.Add(toolMessage);
					}

					response = await llm.ChatStreamingAsync(messages, cts);
					assistantMessage = response.Message;
					messages.Add(assistantMessage);
					sequence.Messages.Add(assistantMessage);
				}
				else
				{
					break;
				}
			}
		}
	}
}