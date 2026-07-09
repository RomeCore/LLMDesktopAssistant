using System.Text;
using System.Text.Json.Nodes;
using AsyncLua;
using AsyncLua.Values;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Utils;
using RCLargeLanguageModels;
using RCLargeLanguageModels.Agents;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Messages.Attachments;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.Scripting.Lua
{
	[LuaApi(chatScoped: true)]
	public class LuaApiAgents : LuaApiBaseAsync
	{
		public override string? Namespace => "dass.agents";

		public override string? Manuals => """
			--- dass.agents — agentic LLM execution API

			Provides the ability to execute LLM agents with tools directly from Lua scripts.
			The agent uses the model configured as "AgenticToolsModel" in chat settings
			and has access to all tools registered in the current chat.

			FUNCTIONS:

			--- async dass.agents.execute(properties...)
			  Executes one or more LLM agents with the given conversations and returns their responses.

			  Supports BATCH EXECUTION: pass multiple property tables to run multiple agents
			  concurrently. Each agent executes independently and the results are returned
			  as an array of response messages. If any of agent execution failed, the failed 
			  agent's response returns error string instead of response messages.

			  Parameters:
				- properties: table (required for each call) — Contains:
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
				  - tools: table (optional) — Mixed array of tool names (strings) and/or
					callback tool definitions (tables). If omitted, no tools are available.

					String entries reference registered tools by name:
					  { "web-search", "fs-read_entry" }

					Table entries define ad-hoc Lua callback tools:
					  {
						name = "my_tool",
						description = "Does something useful.",
						parameters = {
						  type = "object",
						  properties = {
							x = { type = "number" },
							y = { type = "number" }
						  },
						  required = { "x", "y" }
						},
						callback = function(args)
						  return "Result: " .. (args.x + args.y)
						end
					  }

					Mixed example: { "web-search", { name = "calc", ... } }

			  Returns:
				- If a single property table is passed: table — array of response messages
				  (same format as input messages).
				- If multiple property tables are passed (batch): table — array of response arrays,
				  one per input property table.

			  Throws an error if:
				- the agentic model is not configured or the specified model is not found
				- the last message is not a "user" message
				- any message has an unknown role
				- any of the property tables is invalid (in batch mode, all errors are collected)

			  Use pcall() for safe error handling.

			EXAMPLES:

			  -- Simple greeting
			  local r = await dass.agents.execute({
				messages = {
				  { role = "system", content = "You are a helpful assistant." },
				  { role = "user", content = "Say hello!" }
				}
			  })
			  print(table.last(r).content)

			  -- With custom model and tools
			  local r = await dass.agents.execute({
				messages = {
				  { role = "system", content = "You can use tools." },
				  { role = "user", content = "What is 2+2?" }
				},
				model = "openrouter$google/gemini-3.5-flash",
				tools = { "math-calculate" }
			  })
			  print(table.last(r).content)

			  -- Multi-turn with tools
			  local r = await dass.agents.execute({
				messages = {
				  { role = "system", content = "You can use web-search." },
				  { role = "user", content = "Search for latest news about AI" }
				},
				tools = { "web-search" }
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
			  local r = await dass.agents.execute({
				messages = {
				  { role = "system", content = "You are image description assistant." },
				  { role = "user", content = "Describe this image.", attachments = { image.load("image.png") } }
				},
				model = "openrouter$google/gemini-3.5-flash" -- Use a vision model
			  })
			  print(table.last(r).content)

			  -- Safe execution with try-catch
			  try
				local result = await dass.agents.execute({
				  messages = {
					{ role = "system", content = "You are an expert." },
					{ role = "user", content = "What is 2+2?" }
				  }
				})
				print("Answer:", table.last(r).content)
			  catch error do
				print("Failed:", error)
			  end

			  -- Custom callback tool
			  local r = await dass.agents.execute({
				messages = {
				  { role = "system", content = "Use the calculator tool for math." },
				  { role = "user", content = "What is 123 * 456?" }
				},
				tools = {
				  {
					name = "calculator",
					description = "Multiplies two integers.",
					parameters = {
					  type = "object",
					  properties = {
						a = { type = "number", description = "First number" },
						b = { type = "number", description = "Second number" }
					  },
					  required = { "a", "b" }
					},
					callback = function(args)
					  local result = args.a * args.b
					  return tostring(result)
					end
				  }
				}
			  })
			  print(table.last(r).content)  -- "123 * 456 = 56088"

			  -- Batch execution: run multiple agents concurrently
			  local results = await dass.agents.execute(
				{
				  messages = {
					{ role = "system", content = "You are a poet." },
					{ role = "user", content = "Write a haiku about coding." }
				  }
				},
				{
				  messages = {
					{ role = "system", content = "You are a comedian." },
					{ role = "user", content = "Tell me a programming joke." }
				  }
				},
				{
				  messages = {
					{ role = "system", content = "You are a helpful assistant." },
					{ role = "user", content = "Your response will be failed!" }
				  }
				}
			  )
			  -- results[1] is the poet's response array, results[2] is the comedian's, and results[3] contain the error message as a string
			  print("Haiku:", table.last(results[1]).content)
			  print("Joke:", table.last(results[2]).content)
			  print("Failed:", results[3]) -- Error message
			  
			  -- Note: arguments for batch execution can be passed in two ways:
			  -- 1. As separate arguments:
			  dass.agents.execute(batch1, batch2, batch3, ...)
			  -- 2. As an array of tables:
			  dass.agents.execute({batch1, batch2, batch3, ...})

			NOTES:
			  - By default, the agent uses the chat's "AgenticToolsModel" setting.
			  - You can override the model by passing a "model" field in options.
			  - No tools are available by default; you must explicitly pass them using the "tools" field.
			  - Use `table.last` to access the last message in a response array,
				so you can ignore useless messages with tool calls.
			  - CALLBACK TOOLS: pass table entries in the "tools" array with:
				name (string), description (string), parameters (JSON Schema table),
				and callback (function). The callback receives a table of arguments
				matching the schema and should return a string. Callbacks can use the full Lua API (fs, web,
				dass.*, etc.).
			  - Image attachments can be applied via `image` API (see manuals for details).
			  - Returns the full conversation history produced by the agent,
				including all intermediate tool calls and their results.
			  - BATCH EXECUTION: pass multiple property tables to `execute()` to run
				multiple agents concurrently. Each call is independent and errors
				in one do not affect others. The function will throw exception only when
				there's error in agent's parameters, the runtime errors just returns strings.
			""";

		private readonly Chat _chat;
		private readonly LLModelListService _modelList;
		private readonly IToolsetCacheService _toolsetCache;
		private LuaService _luaService = null!;

		public LuaApiAgents(Chat chat, LLModelListService modelList, IToolsetCacheService toolsetCache)
		{
			_chat = chat;
			_modelList = modelList;
			_toolsetCache = toolsetCache;
		}

		public override void Populate(LuaTable globals, LuaTable ns, LuaService luaService)
		{
			_luaService = luaService;
			ns["execute"] = new LuaCallbackFunction(ExecuteAsync);
		}

		private async Task<LuaTuple> ExecuteAsync(LuaCallingContext ctx, LuaValue[] args)
		{
			if (args.Length < 1)
				throw new LuaRuntimeException("dass.agents.execute(properties...): at least 1 argument expected.");
			for (int i = 0; i < args.Length; i++)
				if (args[i].Type != LuaType.Table)
					throw new LuaRuntimeException("dass.agents.execute(): all arguments must be tables.");

			List<Func<Task<LuaTable>>> executionFunctions = [];
			List<Exception?> exceptions = [];

			for (int i = 0; i < args.Length; i++)
			{
				var arg = (LuaTable)args[i];
				if (arg.IsArrayTable())
				{
					foreach (var item in arg.Values)
					{
						try
						{
							var executionFunction = PrepareExecutionFunction(ctx, arg);
							executionFunctions.Add(executionFunction);
							exceptions.Add(null);
						}
						catch (Exception ex)
						{
							exceptions.Add(ex);
						}
					}
				}
				else
				{
					try
					{
						var executionFunction = PrepareExecutionFunction(ctx, arg);
						executionFunctions.Add(executionFunction);
						exceptions.Add(null);
					}
					catch (Exception ex)
					{
						exceptions.Add(ex);
					}
				}
			}

			var nonNullExceptions = exceptions.Where(ex => ex != null).ToList();
			if (nonNullExceptions.Count > 0)
				throw new LuaRuntimeException($"dass.agents.execute(): {string.Join(", ", nonNullExceptions.Select(e => e!.Message))}");

			var tasks = executionFunctions.Select(f => f()).ToList();

			if (tasks.Count == 1)
			{
				try
				{
					var task = tasks[0];
					return new LuaTuple(await task);
				}
				catch (Exception ex)
				{
					throw new LuaRuntimeException($"dass.agents.execute(): {ex.Message}");
				}
			}
			else
			{
				try
				{
					var result = new LuaTable();

					for (int i = 0; i < tasks.Count; i++)
					{
						var task = tasks[i];
						try
						{
							result.Append(await task);
						}
						catch (Exception ex)
						{
							result.Append(new LuaString(ex.Message));
						}
					}

					return new LuaTuple(result);
				}
				catch (Exception ex)
				{
					throw new LuaRuntimeException($"dass.agents.execute(): {ex.Message}");
				}
			}
		}

		private Func<Task<LuaTable>> PrepareExecutionFunction(LuaCallingContext ctx, LuaTable parameters)
		{
			var messagesArg = parameters.Get("messages");
			if (messagesArg is not LuaTable messagesTable)
				throw new Exception("'messages' must be a table.");

			var systemMessageBuilder = new StringBuilder();
			var messages = new List<IMessage>();

			for (int i = 1; i <= messagesTable.Length; i++)
			{
				var _messageTable = messagesTable.Get(i);
				if (_messageTable is not LuaTable messageTable)
					throw new Exception("each message must be a table.");
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

			// Resolve model name and descriptor.
			LLModelDescriptor? model;
			var modelName = parameters.Get("model")?.TryToString();
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

			// Resolve tools: mixed array of strings (registered tools) and tables (callback tools)
			HashSet<string>? toolFilter = null;
			List<(string Name, string Description, JsonNode? Schema, LuaFunction Callback)> callbackToolDefs = [];
			var toolsOption = parameters.Get("tools");
			if (toolsOption is LuaTable toolsOptionTable)
			{
				toolFilter = new HashSet<string>();
				foreach (var toolValue in toolsOptionTable.Values)
				{
					if (toolValue is LuaString toolValueString)
					{
						toolFilter.Add(toolValueString.Value);
					}
					else if (toolValue is LuaTable toolValueTable)
					{
						var cbName = toolValueTable.Get("name").ToString();
						var cbDesc = toolValueTable.Get("description").ToString();
						var cbSchema = StructuredLuaConverter.LuaValueToJsonNode(toolValueTable.Get("parameters"));
						var cbCallback = toolValueTable.Get("callback");

						if (string.IsNullOrEmpty(cbName))
							throw new Exception("callback tool definition: 'name' is required.");
						if (cbCallback is not LuaFunction cbFunc)
							throw new Exception($"callback tool '{cbName}': 'callback' must be a function.");

						callbackToolDefs.Add((cbName, cbDesc ?? cbName, cbSchema, cbFunc));
					}
				}
			}

			var tools = new List<ITool>();

			// Registered tools (filtered)
			foreach (var (_, tool) in _toolsetCache.AvailableTools)
			{
				if (toolFilter == null || !toolFilter.Contains(tool.Name))
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

			// Callback tools (Lua functions)
			foreach (var (cbName, cbDesc, cbSchema, cbCallback) in callbackToolDefs)
			{
				var capturedName = cbName;
				var capturedDesc = cbDesc;
				var capturedSchema = cbSchema;
				var capturedCallback = cbCallback;

				tools.Add(new FunctionTool(capturedName, capturedDesc, capturedSchema?.AsObject() ?? new JsonObject(),
					async (jsonArgs, ct) =>
				{
					try
					{
						// Convert JSON args → Lua table
						var luaArgs = StructuredLuaConverter.JsonNodeToLuaValue(jsonArgs);

						LuaValue luaResult;
						try
						{
							luaResult = await capturedCallback.InvokeAsync(ctx, luaArgs);
						}
						catch (Exception ex)
						{
							return new ToolResult(ToolResultStatus.Error, $"Error occured while executing callback tool '{capturedName}': " + ex.Message);
						}

						string content;
						if (luaResult is LuaString str)
							content = str.Value;
						else if (luaResult is LuaNil)
							content = string.Empty;
						else
							content = luaResult.ToString();

						if (string.IsNullOrEmpty(content))
							content = "Tool executed successfully.";

						return new ToolResult(ToolResultStatus.Success, content);
					}
					catch (Exception ex)
					{
						return new ToolResult(ToolResultStatus.Error, $"Error in callback tool '{capturedName}': {ex.Message}");
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

			async Task<LuaTable> ExecuteAgent()
			{
				var reseivedMessages = new List<IMessage>();
				toolExecutor.MessageReceived += (_, msg) =>
				{
					if (msg != userMessage)
						reseivedMessages.Add(msg);
				};
				await toolExecutor.GenerateResponseAsync(userMessage);

				var resultTable = new LuaTable();

				int im = 1;
				foreach (var message in reseivedMessages)
					resultTable.Set(im++, ConvertMessageToLua(message));

				return resultTable;
			}

			return ExecuteAgent;
		}

		private static IMessage ConvertMessageFromLua(LuaTable messageTable)
		{
			var role = messageTable.Get("role").ToString();
			var content = messageTable.Get("content").ToString();

			switch (role)
			{
				case "system":
					return new RCLargeLanguageModels.Messages.SystemMessage(content);

				case "user":
					var attachmentsTable = messageTable.Get("attachments");
					var attachments = new List<IAttachment>();
					foreach (var attachmentValue in (attachmentsTable as LuaTable)?.Values ?? [])
					{
						if ((attachmentValue as LuaUserData)?.Target is LuaImage image)
							attachments.Add(new ImageBase64Attachment(image.Format, image.ToBase64()));
					}
					return new RCLargeLanguageModels.Messages.UserMessage(Senders.User, content, attachments);

				case "assistant":
					var reasoningContent = messageTable.Get("reasoning_content").ToString();

					var toolCallsTable = messageTable.Get("tool_calls");
					var toolCalls = new List<IToolCall>();
					foreach (var toolCallTable in (toolCallsTable as LuaTable)?.Values ?? [])
						toolCalls.Add(ConvertToolCallFromLua((LuaTable)toolCallTable));

					attachmentsTable = messageTable.Get("attachments");
					attachments = new List<IAttachment>();
					foreach (var attachmentValue in (attachmentsTable as LuaTable)?.Values ?? [])
					{
						if ((attachmentValue as LuaUserData)?.Target is LuaImage image)
							attachments.Add(new ImageBase64Attachment(image.Format, image.ToBase64()));
					}

					return new RCLargeLanguageModels.Messages.AssistantMessage(content, reasoningContent, toolCalls, attachments);

				case "tool":
					var toolName = messageTable.Get("tool_name").ToString();
					var toolCallId = messageTable.Get("tool_call_id").ToString();

					attachmentsTable = messageTable.Get("attachments");
					attachments = new List<IAttachment>();
					foreach (var attachmentValue in (attachmentsTable as LuaTable)?.Values ?? [])
					{
						if ((attachmentValue as LuaUserData)?.Target is LuaImage image)
							attachments.Add(new ImageBase64Attachment(image.Format, image.ToBase64()));
					}

					return new RCLargeLanguageModels.Messages.ToolMessage(content, toolCallId, toolName, attachments);

				default:
					throw new LuaRuntimeException($"dass.agents.execute(): unknown role '{role}'.");
			}
		}

		private static IToolCall ConvertToolCallFromLua(LuaTable toolCallTable)
		{
			var toolName = toolCallTable.Get("tool_name").ToString();
			var toolCallId = toolCallTable.Get("tool_call_id").ToString();
			var arguments = StructuredLuaConverter.LuaValueToJsonNode(toolCallTable.Get("arguments"));
			return new FunctionToolCall(toolCallId, toolName, arguments?.ToJsonString() ?? "{}");
		}

		private static LuaTable ConvertMessageToLua(IMessage message)
		{
			switch (message)
			{
				case IUserMessage userMessage:
					var userMessageTable = new LuaTable();
					userMessageTable.Set("role", "user");
					userMessageTable.Set("content", userMessage.Content);
					return userMessageTable;

				case IAssistantMessage assistantMessage:
					var assistantMessageTable = new LuaTable();
					assistantMessageTable.Set("role", "assistant");
					assistantMessageTable.Set("reasoning_content", assistantMessage.ReasoningContent);
					assistantMessageTable.Set("content", assistantMessage.Content);

					int i = 1;
					var toolCallsTable = new LuaTable();
					foreach (var toolCall in assistantMessage.ToolCalls)
						toolCallsTable.Set(i++, ConvertToolCallToLua(toolCall));
					assistantMessageTable["tool_calls"] = toolCallsTable;

					return assistantMessageTable;

				case IToolMessage toolMessage:
					var toolMessageTable = new LuaTable();
					toolMessageTable.Set("role", "tool");
					toolMessageTable.Set("tool_name", toolMessage.ToolName);
					toolMessageTable.Set("tool_call_id", toolMessage.ToolCallId);
					toolMessageTable.Set("content", toolMessage.Content);
					return toolMessageTable;

				default:
					throw new LuaRuntimeException($"dass.agents.execute(): unknown message '{message}'.");
			}
		}

		private static LuaTable ConvertToolCallToLua(IToolCall toolCall)
		{
			if (toolCall is not FunctionToolCall functionCall)
				throw new LuaRuntimeException($"dass.agents.execute(): tool call is not function tool call: '{toolCall}'.");

			var resultTable = new LuaTable();
			resultTable.Set("tool_name", functionCall.ToolName);
			resultTable.Set("tool_call_id", functionCall.Id);
			resultTable.Set("arguments", StructuredLuaConverter.JsonNodeToLuaValue(TolerantJsonParser.Parse(functionCall.Args)));
			return resultTable;
		}
	}
}