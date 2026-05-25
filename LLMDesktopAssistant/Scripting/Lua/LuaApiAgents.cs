using System;
using System.Collections.Generic;
using System.Text;
using LLMDesktopAssistant.Services.Instances;
using MoonSharp.Interpreter;
using RCLargeLanguageModels;
using RCLargeLanguageModels.Agents;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.Scripting.Lua
{
	public class LuaApiAgents : LuaApiBase
	{
		public override string? Namespace => "dass.agents";

		private readonly LLModelListService _modelList;

		public LuaApiAgents(LLModelListService modelList)
		{
			_modelList = modelList;
		}

		public override void Populate(Table globals, Table ns)
		{
			ns["execute"] = DynValue.NewCallback(Execute);
		}

		private DynValue Execute(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("dass.agents.execute(messages, [params]): at least 1 argument expected.");
			var messagesArg = args[0];
			if (messagesArg.Type != DataType.Table)
				throw new ScriptRuntimeException("dass.agents.execute(): first argument must be a table.");

			var messagesTable = messagesArg.Table;
			var systemMessage = new StringBuilder();
			var messages = new List<IMessage>();

			for (int i = 1; i <= messagesTable.Length; i++)
			{
				var _messageTable = messagesTable.Get(i);
				if (_messageTable.Type != DataType.Table)
					throw new ScriptRuntimeException("dass.agents.execute(): each message must be a table.");
				var messageTable = _messageTable.Table;

			}

			var memory = new SlidingChatMemory
			{
				ReturnLastNMessages = -1
			};


			return DynValue.Nil;
		}

		private static IMessage ConvertMessageFromLua(Table messageTable)
		{
			var role = messageTable.Get("role").CastToString();
			var content = messageTable.Get("content").CastToString();

			switch (role)
			{
				case "system":
					return new SystemMessage(content);

				case "user":
					return new UserMessage(content);

				case "assistant":
					var reasoningContent = messageTable.Get("reasoning_content").CastToString();
					var toolCallsTable = messageTable.Get("tool_calls");
					return new AssistantMessage(content, reasoningContent, toolCalls);

				case "tool":
					var toolCallId = messageTable.Get("tool_call_id").CastToString();
					var toolName = messageTable.Get("tool_name").CastToString();
					return new ToolMessage(content, toolCallId, toolName);

				default:
					throw new ScriptRuntimeException($"dass.agents.execute(): unknown role '{role}'.");
			}
		}

		private static IToolCall ConvertToolCallFromLua(Table toolCallTable)
		{

		}
	}
}