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

namespace LLMDesktopAssistant.LLM
{
	[ViewModelFor(typeof(ChatView))]
	[TabTool("chat")]
	public class ChatViewModel : ViewModelBase
	{
		private CancellationTokenSource? _sendCts;

		private ObservableCollection<ConversationTurnViewModel> _turns = [];
		/// <summary>
		/// Gets or sets the collection of conversation turns.
		/// </summary>
		public ObservableCollection<ConversationTurnViewModel> Turns
		{
			get => _turns;
			set => SetProperty(ref _turns, value);
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

			var userMessage = new UserMessage(UserInput);

			var messages = new List<IMessage>();
			messages.Add(new SystemMessage(GetSystemMessageContent()));
			messages.AddRange(Turns.SelectMany(turn => turn.Messages));
			messages.Add(userMessage);

			var llm = ModuleManager.GetDynamic<ILLMProvider>().GetLLM();

			var allTools = new ToolSet(ModuleManager.GetAll<ToolModule>()
				.Where(t => t.Enabled)
				.SelectMany(t => t.GetTools()));
			// Add existing LLM's tools.
			foreach (var tool in llm.Tools)
				allTools.Add(tool);
			llm = llm.WithTools(allTools);

			var response = await llm.ChatStreamingAsync(messages, cts);
			var assistantMessage = response.Message;
			messages.Add(assistantMessage);

			var turn = new ConversationTurnViewModel();
			Turns.Add(turn);
			UserInput = string.Empty;

			InvokeUI(() =>
			{
				turn.Messages.Add(userMessage);
				turn.UserMessage = new UserMessageViewModel(userMessage);
			});

			void AddAssistantMessage(PartialAssistantMessage assistantMessage)
			{
				InvokeUI(() =>
				{
					turn.Messages.Add(assistantMessage);
					if (!string.IsNullOrEmpty(assistantMessage.ReasoningContent))
					{
						turn.AssistantMessageParts.Add(new AssistantMessageReasoningPartViewModel(assistantMessage));
					}
					if (!string.IsNullOrEmpty(assistantMessage.Content))
					{
						turn.AssistantMessageParts.Add(new AssistantMessageTextPartViewModel(assistantMessage));
					}
				});

				bool hasReasoning = false, hasContent = false;

				void PartHandler(object? s, AssistantMessageDelta e)
				{
					InvokeUI(() =>
					{
						if (!hasReasoning && !string.IsNullOrEmpty(assistantMessage.ReasoningContent))
						{
							turn.AssistantMessageParts.Add(new AssistantMessageReasoningPartViewModel(assistantMessage));
							hasReasoning = true;
						}
						if (!hasContent && !string.IsNullOrEmpty(assistantMessage.Content))
						{
							turn.AssistantMessageParts.Add(new AssistantMessageTextPartViewModel(assistantMessage));
							hasContent = true;
						}
					});
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
			}

			AddAssistantMessage(assistantMessage);

			while (true)
			{
				ConcurrentDictionary<IToolCall, IToolMessage> toolMessageMap = [];
				List<Task> toolExecutionTasks = [];
				AssistantMessageToolPartViewModel? toolPart = null;

				void ProcessToolCall(IToolCall toolCall)
				{
					switch (toolCall)
					{
						case FunctionToolCall functionCall:

							var tool = allTools.Get(toolCall.ToolName) as FunctionTool ??
								throw new InvalidOperationException($"FunctionTool '{functionCall.ToolName}' not found in the current toolset.");

							var toolCallVm = new ToolCallViewModel
							{
								ToolName = toolCall.ToolName,
								Status = ToolCallStatus.InProgress
							};
							InvokeUI(() =>
							{
								if (toolPart == null)
								{
									toolPart = new AssistantMessageToolPartViewModel();
									turn.AssistantMessageParts.Add(toolPart);
								}
								toolPart.ToolCalls.Add(toolCallVm);
							});

							Log.Debug("LLM called function '{name}' (tool call id: {id}) with arguments: {args}.",
								toolCall.ToolName, toolCall.Id, functionCall.Args.ToString());

							var executionTask = tool.ExecuteAsync(functionCall.Args, cts)
								.ContinueWith(t =>
								{
									InvokeUI(() =>
									{
										if (t.IsCanceled)
											toolCallVm.Status = ToolCallStatus.Failure;
										else if (t.IsFaulted)
											toolCallVm.Status = ToolCallStatus.Failure;
										else
											toolCallVm.Status = ToolCallStatus.Success;
									});

									ToolResult toolMsgContent;

									if (t.IsCanceled)
										toolMsgContent = "CANCELLED";
									else if (t.IsFaulted)
										toolMsgContent = "ERROR";
									else
										toolMsgContent = t.Result;

									Log.Debug("LLM function '{name}' (tool call id: {id}) completed with result: '{result}'.",
										toolCall.ToolName, toolCall.Id, toolMsgContent.Content);

									toolMessageMap[toolCall] = new ToolMessage(toolMsgContent, toolCall.Id, toolCall.ToolName);
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
				Log.Debug("Received message: {msg}", assistantMessage.Content);

				if (toolExecutionTasks.Count > 0)
				{
					await Task.WhenAll(toolExecutionTasks);

					// Send the tool results back to the LLM.

					foreach (var toolCall in assistantMessage.ToolCalls)
					{
						var toolMessage = toolMessageMap[toolCall];
						messages.Add(toolMessage);
						InvokeUI(() =>
						{
							turn.Messages.Add(toolMessage);
						});
					}

					response = await llm.ChatStreamingAsync(messages, cts);
					assistantMessage = response.Message;
					messages.Add(assistantMessage);

					AddAssistantMessage(assistantMessage);
				}
				else
				{
					break;
				}
			}
		}

		private static void InvokeUI(Action action)
		{
			App.Current.Dispatcher.Invoke(action);
		}

		private static string GetSystemMessageContent()
		{
			return $"""
				You are a helpful assistant.

				Current date and time: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}.
				""";
		}
	}
}