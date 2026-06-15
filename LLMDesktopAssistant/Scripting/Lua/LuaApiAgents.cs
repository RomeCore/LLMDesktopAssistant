using System.Text;
using System.Text.Json.Nodes;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Tools;
using MoonSharp.Interpreter;
using RCLargeLanguageModels;
using RCLargeLanguageModels.Agents;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Messages.Attachments;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.Scripting.Lua
{
	[ChatService(typeof(LuaApiBase))]
	public class LuaApiAgents : LuaApiBase
	{
		public override string? Namespace => "dass.agents";

		public override string? Manuals => """
			--- dass.agents — agentic LLM execution API

			Provides the ability to execute LLM agents with tools directly from Lua scripts.
			The agent uses the model configured as "AgenticToolsModel" in chat settings
			and has access to all tools registered in the current chat.

			FUNCTIONS:

			--- dass.agents.execute(properties...)
			  Executes an LLM agent with the given conversation and returns its response.

			  Parameters:
				- properties: table (required) — Additional options:
				  - messages: table (required) — Array of message tables (see format below).
					The LAST message MUST be a "user" message.
					Multiple "system" messages are concatenated.
			
					Each message table has a "role" field:
					role = "system":
					  - content: string — system instruction
			
					role = "user":
					  - content: string — user message text
					  - attachments: table (optional) — array of attachment objects (see format below)
			
					role = "assistant":
					  - content: string — assistant response text
					  - reasoning_content: string (optional) — reasoning/thinking text
					  - tool_calls: table (optional) — array of tool call tables
					  Each tool call:
						- tool_name: string
						- tool_call_id: string
						- arguments: table — arguments matching the tool's schema
			
					role = "tool":
					  - content: string — tool result text
					  - tool_name: string
					  - tool_call_id: string
			
				  - model: string (optional) — Name of the model to use.
					If omitted, the chat's "AgenticToolsModel" is used.
				  - tools: table (optional) — Array of tool names (strings) to restrict which tools
					the agent can use. If omitted, all available tools are exposed.
					Example: { "web-search", "calculate" }

			  Returns: table — array of response messages (same format as input messages).

			  Throws an error if:
				- the agentic model is not configured or the specified model is not found
				- the last message is not a "user" message
				- any message has an unknown role

			  Use pcall() for safe error handling.

			EXAMPLES:

			  -- Simple greeting
			  local r = dass.agents.execute({
				messages = {
				  { role = "system", content = "You are a helpful assistant." },
				  { role = "user", content = "Say hello!" }
				}
			  })
			  print(r[1].content)

			  -- With custom model and restricted tools
			  local r = dass.agents.execute({
				messages = {
				  { role = "system", content = "You can use tools." },
				  { role = "user", content = "What is 2+2?" }
				},
				model = "openrouter$google/gemini-3.5-flash",
				tools = { "math-calculate" }
			  })

			  -- Multi-turn with tools
			  local r = dass.agents.execute({
				messages = {
				  { role = "system", content = "You can use web-search." },
				  { role = "user", content = "Search for latest news about AI" }
				}
			  })
			  for _, msg in ipairs(r) do
				if msg.role == "assistant" then
				  print("AI:", msg.content)
				  if msg.tool_calls then
					for _, tc in ipairs(msg.tool_calls) do
					  print("  -> tool:", tc.tool_name)
					end
				  end
				elseif msg.role == "tool" then
				  print("  result:", msg.content:sub(1, 100))
				end
			  end

			  -- Attachments with image description
			  local r = dass.agents.execute({
				messages = {
				  { role = "system", content = "You are image description assistant." },
				  { role = "user", content = "Describe this image.", attachments = { image.load("image.png") } }
				},
				model = "openrouter$google/gemini-3.5-flash" -- Use a vision model
			  })
			  print(r[1].content)

			  -- Safe execution with pcall
			  local ok, result = pcall(dass.agents.execute, {
			    messages = {
				  { role = "system", content = "You are an expert." },
				  { role = "user", content = "What is 2+2?" }
				}
			  })
			  if ok then
				print("Answer:", result[1].content)
			  else
				print("Failed:", result)
			  end

			NOTES:
			  - By default, the agent uses the chat's "AgenticToolsModel" setting.
			  - You can override the model by passing a "model" field in options.
			  - You can restrict tools by passing a "tools" array in options.
			  - All tools available in the current chat are exposed to the agent by default.
			  - Image attachments can be applied via `image` API (see manuals for details).
			  - Returns the full conversation history produced by the agent,
				including all intermediate tool calls and their results.
			""";

		private readonly Chat _chat;
		private readonly LLModelListService _modelList;
		private readonly IToolsetCacheService _toolsetCache;

		public LuaApiAgents(Chat chat, LLModelListService modelList, IToolsetCacheService toolsetCache)
		{
			_chat = chat;
			_modelList = modelList;
			_toolsetCache = toolsetCache;
		}

		public override void Populate(Table globals, Table ns, LuaService luaService)
		{
			ns["execute"] = DynValue.NewCallback(Execute);
		}

		private DynValue Execute(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("dass.agents.execute(properties...): at least 1 argument expected.");
			for (int i = 0; i < args.Count; i++)
				if (args[i].Type != DataType.Table)
					throw new ScriptRuntimeException("dass.agents.execute(): all arguments must be tables.");

			var script = ctx.GetScript();
			
			List<Func<Task<DynValue>>> executionFunctions = [];
			List<Exception?> exceptions = [];

			for (int i = 0; i < args.Count; i++)
			{
				var arg = args[i].Table;
				try
				{
					var executionFunction = PrepareExecutionFunction(script, arg);
					executionFunctions.Add(executionFunction);
					exceptions.Add(null);
				}
				catch (Exception ex)
				{
					exceptions.Add(ex);
				}
			}

			var nonNullExceptions = exceptions.Where(ex => ex != null).ToList();
			if (nonNullExceptions.Count > 0)
				throw new ScriptRuntimeException($"dass.agents.execute(): {string.Join(", ", nonNullExceptions.Select(e => e!.Message))}");

			var tasks = executionFunctions.Select(f => f()).ToList();

			if (tasks.Count == 1)
			{
				try
				{
					var task = tasks[0];
					task.Wait();
					return task.Result;
				}
				catch (Exception ex)
				{
					throw new ScriptRuntimeException($"dass.agents.execute(): {ex.Message}");
				}
			}
			else
			{
				try
				{
					var result = new Table(script);

					for (int i = 0; i < tasks.Count; i++)
					{
						var task = tasks[i];
						tasks[0].Wait();
						result.Append(task.Result);
					}

					return DynValue.NewTable(result);
				}
				catch (Exception ex)
				{
					throw new ScriptRuntimeException($"dass.agents.execute(): {ex.Message}");
				}
			}
		}

		private Func<Task<DynValue>> PrepareExecutionFunction(Script script, Table parameters)
		{
			var messagesArg = parameters.Get("messages");
			if (messagesArg.Type != DataType.Table)
				throw new Exception("'messages' must be a table.");

			var messagesTable = messagesArg.Table;
			var systemMessageBuilder = new StringBuilder();
			var messages = new List<IMessage>();

			for (int i = 1; i <= messagesTable.Length; i++)
			{
				var _messageTable = messagesTable.Get(i);
				if (_messageTable.Type != DataType.Table)
					throw new Exception("each message must be a table.");
				var messageTable = _messageTable.Table;
				var message = ConvertMessageFromLua(messageTable);
				if (message is ISystemMessage systemMessage)
					systemMessageBuilder.AppendLine(systemMessage.Content);
				else
					messages.Add(message);
			}

			if (messages.Count == 0 || messages[^1] is not RCLargeLanguageModels.Messages.IUserMessage)
				throw new Exception("last message must be an user message.");

			var memory = new SlidingChatMemory
			{
				ReturnLastNMessages = -1,
				SystemInstructions = systemMessageBuilder.ToString().Trim(),
				Messages = messages.Take(messages.Count - 1).ToList(),
			};

			// Resolve model
			LLModelDescriptor? model;
			var modelName = parameters.Get("model")?.CastToString();
			if (!string.IsNullOrEmpty(modelName))
			{
				var tracked = _modelList.Registry.GetModel(modelName);
				model = tracked?.Current;
				if (model is null)
					throw new Exception($"model '{modelName}' not found.");
			}
			else
			{
				model = _chat.Settings.Models.AgenticToolsModel.Current;
				if (model is null)
					throw new Exception("agentic model is not available.");
			}

			// Resolve tool filter
			HashSet<string>? toolFilter = null;
			var toolsOption = parameters.Get("tools");
			if (toolsOption.Type == DataType.Table)
			{
				toolFilter = new HashSet<string>();
				foreach (var toolValue in toolsOption.Table.Values)
				{
					var toolName = toolValue.CastToString();
					if (toolName != null)
						toolFilter.Add(toolName);
				}
			}

			var tools = new List<ITool>();
			foreach (var (_, tool) in _toolsetCache.AvailableTools)
			{
				if (toolFilter != null && !toolFilter.Contains(tool.Name))
					continue;

				var _tool = tool;
				tools.Add(new FunctionTool(_tool.Name, _tool.DescriptionGetter(), _tool.ArgumentSchema, async (args, ct) =>
				{
					try
					{
						var dummyCtx = ToolExecutionContext.CreateDummy(_tool, args, _chat);
						var result = await _tool.Executor(args, dummyCtx, ct);
						var success = await result.Completion;
						var content = result.ResultContent;
						if (string.IsNullOrEmpty(content))
							content = "Tool did not returned any result.";
						return new ToolResult(success ? ToolResultStatus.Success : ToolResultStatus.Error, content);
					}
					catch (Exception ex)
					{
						return new ToolResult(ToolResultStatus.Error, $"Error occured while executing tool: " + ex.Message);
					}
				}));
			}

			var toolExecutor = new LLMToolExecutor
			{
				Memory = memory,
				LLMProvider = new LLModel(model).WithTools(tools),
				MaxParallelToolExecutions = -1,
				MaxToolCycles = -1,
				MaxToolCalls = -1
			};

			var userMessage = (RCLargeLanguageModels.Messages.IUserMessage)messages[^1];

			async Task<DynValue> ExecuteAgent()
			{
				var reseivedMessages = new List<IMessage>();
				toolExecutor.MessageReceived += (_, msg) =>
				{
					if (msg != userMessage)
						reseivedMessages.Add(msg);
				};
				await toolExecutor.GenerateResponseAsync(userMessage);

				var resultTable = new Table(script);

				int im = 1;
				foreach (var message in reseivedMessages)
					resultTable.Set(im++, ConvertMessageToLua(message, script));

				return DynValue.NewTable(resultTable);
			}

			return ExecuteAgent;
		}

		private static IMessage ConvertMessageFromLua(Table messageTable)
		{
			var role = messageTable.Get("role").CastToString();
			var content = messageTable.Get("content").CastToString();

			switch (role)
			{
				case "system":
					return new RCLargeLanguageModels.Messages.SystemMessage(content);

				case "user":
					var attachmentsTable = messageTable.Get("attachments");
					var attachments = new List<IAttachment>();
					foreach (var attachmentValue in attachmentsTable.Table?.Values ?? [])
					{
						if (attachmentValue.UserData?.Object is LuaImage image)
							attachments.Add(new ImageBase64Attachment(image.Format, image.ToBase64()));
					}
					return new RCLargeLanguageModels.Messages.UserMessage(Senders.User, content, attachments);

				case "assistant":
					var reasoningContent = messageTable.Get("reasoning_content").CastToString();

					var toolCallsTable = messageTable.Get("tool_calls");
					var toolCalls = new List<IToolCall>();
					foreach (var toolCallTable in toolCallsTable.Table?.Values ?? [])
						toolCalls.Add(ConvertToolCallFromLua(toolCallTable.Table));

					attachmentsTable = messageTable.Get("attachments");
					attachments = new List<IAttachment>();
					foreach (var attachmentValue in attachmentsTable.Table?.Values ?? [])
					{
						if (attachmentValue.UserData?.Object is LuaImage image)
							attachments.Add(new ImageBase64Attachment(image.Format, image.ToBase64()));
					}

					return new RCLargeLanguageModels.Messages.AssistantMessage(content, reasoningContent, toolCalls, attachments);

				case "tool":
					var toolName = messageTable.Get("tool_name").CastToString();
					var toolCallId = messageTable.Get("tool_call_id").CastToString();

					attachmentsTable = messageTable.Get("attachments");
					attachments = new List<IAttachment>();
					foreach (var attachmentValue in attachmentsTable.Table?.Values ?? [])
					{
						if (attachmentValue.UserData?.Object is LuaImage image)
							attachments.Add(new ImageBase64Attachment(image.Format, image.ToBase64()));
					}

					return new RCLargeLanguageModels.Messages.ToolMessage(content, toolCallId, toolName, attachments);

				default:
					throw new ScriptRuntimeException($"dass.agents.execute(): unknown role '{role}'.");
			}
		}

		private static IToolCall ConvertToolCallFromLua(Table toolCallTable)
		{
			var toolName = toolCallTable.Get("tool_name").CastToString();
			var toolCallId = toolCallTable.Get("tool_call_id").CastToString();
			var arguments = JsonLuaConverter.DynValueToJsonNode(toolCallTable.Get("arguments")) ?? JsonValue.Create((string?)null)!;
			return new FunctionToolCall(toolCallId, toolName, arguments.ToJsonString());
		}

		private static DynValue ConvertMessageToLua(IMessage message, Script script)
		{
			switch (message)
			{
				case IUserMessage userMessage:
					var userMessageTable = new Table(script);
					userMessageTable["role"] = "user";
					userMessageTable["content"] = userMessage.Content;
					return DynValue.NewTable(userMessageTable);

				case IAssistantMessage assistantMessage:
					var assistantMessageTable = new Table(script);
					assistantMessageTable["role"] = "assistant";
					assistantMessageTable["reasoning_content"] = assistantMessage.ReasoningContent;
					assistantMessageTable["content"] = assistantMessage.Content;

					int i = 1;
					var toolCallsTable = new Table(script);
					foreach (var toolCall in assistantMessage.ToolCalls)
						toolCallsTable.Set(i++, ConvertToolCallToLua(toolCall, script));
					assistantMessageTable["tool_calls"] = toolCallsTable;

					return DynValue.NewTable(assistantMessageTable);

				case IToolMessage toolMessage:
					var toolMessageTable = new Table(script);
					toolMessageTable["role"] = "tool";
					toolMessageTable["tool_name"] = toolMessage.ToolName;
					toolMessageTable["tool_call_id"] = toolMessage.ToolCallId;
					toolMessageTable["content"] = toolMessage.Content;
					return DynValue.NewTable(toolMessageTable);

				default:
					throw new ScriptRuntimeException($"dass.agents.execute(): unknown message '{message}'.");
			}
		}

		private static DynValue ConvertToolCallToLua(IToolCall toolCall, Script script)
		{
			if (toolCall is not FunctionToolCall functionCall)
				throw new ScriptRuntimeException($"dass.agents.execute(): tool call is not function tool call: '{toolCall}'.");

			var resultTable = new Table(script);
			resultTable["tool_name"] = functionCall.ToolName;
			resultTable["tool_call_id"] = functionCall.Id;
			resultTable["arguments"] = JsonLuaConverter.JsonNodeToDynValue(script, functionCall.Args);
			return DynValue.NewTable(resultTable);
		}
	}
}