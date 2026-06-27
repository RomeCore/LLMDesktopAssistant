using System.Text.Json.Nodes;
using LLMDesktopAssistant.Tools;
using Material.Icons;
using ModelContextProtocol.Protocol;
using MoonSharp.Interpreter;

namespace LLMDesktopAssistant.Scripting.Lua
{
	/*

	/// <summary>
	/// Lua API for reactive tool result: <c>dass.tool.result.*</c>.
	/// Provides access to the current tool's <see cref="ReactiveToolResult"/>
	/// for streaming output, progress updates, status icons, and completion control.
	/// </summary>
	[LuaApi(chatScoped: false)]
	public class LuaApiToolResult : LuaApiBase
	{
		public override string? Namespace => "dass.tool.result";

		public override string? Manuals => """
			--- dass.tool.result — reactive tool result API

			Provides access to the current tool's reactive result for streaming
			output, progress updates, and completion control.
			Available only inside Lua scripts executed as tools or meta-tools.

			FUNCTIONS:
			
			--- dass.tool.result.available()
			  Checks whether a tool execution context is available (i.e. we are inside a running tool).
			  Returns: boolean — true if dass.tool.result functions can be used
			
			--- dass.tool.result.get()
			  Returns the current reactive result object (table), or nil if not in a tool context.
			  The returned table has fields (read-only):
			    - content: string — current accumulated output
			    - is_completed: boolean — whether execution has finished
			    - success: boolean or nil — result status (nil if not completed)
			    - progress: number or nil — current progress (nil = indeterminate)
			    - progress_min: number — minimum progress value (default 0.0)
			    - progress_max: number — maximum progress value (default 1.0)
			    - status_icon: string or nil — current status icon name
			    - status_title: string or nil — current status title
			    - use_markdown: boolean — whether content is rendered as Markdown

			--- dass.tool.result.write(line)
			  Appends a line of text to the result output.
			  Parameters:
			    - line: string — line to append

			--- dass.tool.result.write_lines(lines)
			  Appends multiple lines to the result output at once.
			  Parameters:
			    - lines: table — array of strings

			--- dass.tool.result.set_content(text)
			  Replaces the entire result content with the given text.
			  Parameters:
			    - text: string — new content

			--- dass.tool.result.set_progress(value, [min], [max])
			  Sets the progress of the tool execution with optional range limits.
			  Parameters:
			    - value: number or nil — progress value, or nil for indeterminate
			    - min: number (optional) — minimum progress value (default: 0.0)
			    - max: number (optional) — maximum progress value (default: 1.0)

			--- dass.tool.result.get_progress()
			  Returns the current progress value.
			  Returns: table with fields:
			    - value: number or nil — current progress (nil = indeterminate)
			    - min: number — minimum progress value
			    - max: number — maximum progress value

			--- dass.tool.result.set_status(icon, title)
			  Sets the status icon and title shown in the UI next to the tool name.
			  Parameters:
			    - icon: string — MaterialIconKind name (e.g. "File", "Web", "Check", "Download")
			      Pass empty string or nil to keep current icon.
			    - title: string — status title text (e.g. "Downloading file...")
			      Pass empty string or nil to keep current title.

			--- dass.tool.result.set_structured(data)
			  Sets the structured result data (Lua table → JSON).
			  Parameters:
			    - data: table — structured data to return alongside text content

			--- dass.tool.result.use_markdown(enabled)
			  Sets whether the result content should be rendered as Markdown.
			  Parameters:
			    - enabled: boolean — true to enable Markdown rendering

			--- dass.tool.result.complete([success])
			  Completes the tool execution. After calling this, further writes are ignored.
			  Parameters:
			    - success: boolean (optional) — true for success (default), false for error

			--- dass.tool.result.complete_with_success()
			  Completes the tool execution with success status.

			--- dass.tool.result.complete_with_error()
			  Completes the tool execution with error status.

			--- dass.tool.result.clear()
			  Clears all result content lines.

			EXAMPLES:

			  -- Streaming output with progress
			  dass.tool.result.set_status("Download", "Downloading...")
			  dass.tool.result.set_progress(0.0)
			  dass.tool.result.write("Starting download...")
			  -- ... do work ...
			  dass.tool.result.set_progress(0.5)
			  dass.tool.result.write("Halfway there!")
			  -- ... more work ...
			  dass.tool.result.set_progress(1.0)
			  dass.tool.result.write("Done!")
			  dass.tool.result.complete_with_success()

			  -- Structured result
			  dass.tool.result.set_structured({
			    result = "ok",
			    items = { 1, 2, 3 },
			    metadata = {
			      source = "lua",
			      version = 2
			    }
			  })
			  dass.tool.result.complete(true)

			  -- Error case
			  dass.tool.result.set_status("Alert", "Something went wrong")
			  dass.tool.result.write("Error: " .. tostring(err))
			  dass.tool.result.complete_with_error()

			NOTES:
			  - All functions are no-ops if not in a tool execution context.
			  - After complete(), most setters become ineffective.
			  - Use icon.exists() and icon.list() to explore available icon names.
			""";

		public override void Populate(Table globals, Table ns, LuaService luaService)
		{
			ns["available"] = DynValue.NewCallback(Available);
			ns["get"] = DynValue.NewCallback(Get);
			ns["write"] = DynValue.NewCallback(Write);
			ns["write_lines"] = DynValue.NewCallback(WriteLines);
			ns["set_content"] = DynValue.NewCallback(SetContent);
			ns["set_progress"] = DynValue.NewCallback(SetProgress);
			ns["get_progress"] = DynValue.NewCallback(GetProgress);
			ns["set_status"] = DynValue.NewCallback(SetStatus);
			ns["set_structured"] = DynValue.NewCallback(SetStructured);
			ns["use_markdown"] = DynValue.NewCallback(UseMarkdown);
			ns["complete"] = DynValue.NewCallback(Complete);
			ns["complete_with_success"] = DynValue.NewCallback(CompleteWithSuccess);
			ns["complete_with_error"] = DynValue.NewCallback(CompleteWithError);
			ns["clear"] = DynValue.NewCallback(Clear);
		}

		private static ReactiveToolResult? GetResult(ScriptExecutionContext ctx)
		{
			return ctx.TryGetReactiveToolResult();
		}

		private DynValue Available(ScriptExecutionContext ctx, CallbackArguments args)
		{
			return DynValue.NewBoolean(GetResult(ctx) != null);
		}

		private DynValue Get(ScriptExecutionContext ctx, CallbackArguments args)
		{
			var result = GetResult(ctx);
			if (result == null)
				return DynValue.Nil;

			var script = ctx.GetScript();
			var table = new Table(script);

			table["content"] = result.ResultContent;
			table["is_completed"] = DynValue.NewBoolean(result.Completion.IsCompleted);
			table["success"] = result.Completion.IsCompleted
				? DynValue.NewBoolean(result.Completion.Result)
				: DynValue.Nil;
			table["progress"] = result.Progress.HasValue
				? DynValue.NewNumber(result.Progress.Value)
				: DynValue.Nil;
			table["progress_min"] = DynValue.NewNumber(result.MinProgress);
			table["progress_max"] = DynValue.NewNumber(result.MaxProgress);
			table["status_icon"] = result.StatusIcon?.ToString();
			table["status_title"] = result.StatusTitle;
			table["use_markdown"] = DynValue.NewBoolean(result.UseMarkdown);

			return DynValue.NewTable(table);
		}

		private DynValue Write(ScriptExecutionContext ctx, CallbackArguments args)
		{
			var result = GetResult(ctx);
			var text = args[0].CastToString();
			if (result != null && text != null)
				result.ResultContentLines.Add(text);
			return DynValue.Nil;
		}

		private DynValue WriteLines(ScriptExecutionContext ctx, CallbackArguments args)
		{
			var result = GetResult(ctx);
			if (result == null)
				return DynValue.Nil;

			var lines = args[0];
			if (lines.Type != DataType.Table)
				return DynValue.Nil;

			foreach (var value in lines.Table.Values)
			{
				var text = value.CastToString();
				if (text != null)
					result.ResultContentLines.Add(text);
			}

			return DynValue.Nil;
		}

		private DynValue SetContent(ScriptExecutionContext ctx, CallbackArguments args)
		{
			var result = GetResult(ctx);
			var text = args[0].CastToString();
			if (result != null && text != null)
				result.ResultContent = text;
			return DynValue.Nil;
		}

		private DynValue SetProgress(ScriptExecutionContext ctx, CallbackArguments args)
		{
			var result = GetResult(ctx);
			if (result == null)
				return DynValue.Nil;

			var value = args[0];
			if (value.Type == DataType.Number)
				result.Progress = value.Number;
			else if (value.IsNil())
				result.Progress = null;

			// Optional min/max range
			if (args.Count >= 2 && args[1].Type == DataType.Number)
				result.MinProgress = args[1].Number;
			if (args.Count >= 3 && args[2].Type == DataType.Number)
				result.MaxProgress = args[2].Number;

			return DynValue.Nil;
		}

		private DynValue GetProgress(ScriptExecutionContext ctx, CallbackArguments args)
		{
			var result = GetResult(ctx);
			if (result == null)
				return DynValue.Nil;

			var script = ctx.GetScript();
			var table = new Table(script);
			table["value"] = result.Progress.HasValue
				? DynValue.NewNumber(result.Progress.Value)
				: DynValue.Nil;
			table["min"] = DynValue.NewNumber(result.MinProgress);
			table["max"] = DynValue.NewNumber(result.MaxProgress);
			return DynValue.NewTable(table);
		}

		private DynValue SetStatus(ScriptExecutionContext ctx, CallbackArguments args)
		{
			var result = GetResult(ctx);
			if (result == null)
				return DynValue.Nil;

			var iconArg = args[0].CastToString();
			var titleArg = args[1].CastToString();

			if (!string.IsNullOrEmpty(iconArg))
			{
				try
				{
					var icon = Enum.Parse<MaterialIconKind>(iconArg, ignoreCase: true);
					result.StatusIcon = icon;
				}
				catch
				{
					result.StatusIcon = null;
				}
			}

			if (!string.IsNullOrEmpty(titleArg))
				result.StatusTitle = titleArg;

			return DynValue.Nil;
		}

		private DynValue SetStructured(ScriptExecutionContext ctx, CallbackArguments args)
		{
			var result = GetResult(ctx);
			if (result == null)
				return DynValue.Nil;

			var data = args[0];
			if (data.Type != DataType.Table && !data.IsNil())
				return DynValue.Nil;

			var jsonNode = StructuredLuaConverter.LuaValueToJsonNode(data);
			result.StructuredResult = jsonNode;

			return DynValue.Nil;
		}

		private DynValue UseMarkdown(ScriptExecutionContext ctx, CallbackArguments args)
		{
			var result = GetResult(ctx);
			if (result == null)
				return DynValue.Nil;

			result.UseMarkdown = args[0].CastToBool();
			return DynValue.Nil;
		}

		private DynValue Complete(ScriptExecutionContext ctx, CallbackArguments args)
		{
			var result = GetResult(ctx);
			if (result == null)
				return DynValue.Nil;

			var success = args[0].IsNil() || args[0].CastToBool();
			result.TryComplete(success);

			return DynValue.Nil;
		}

		private DynValue CompleteWithSuccess(ScriptExecutionContext ctx, CallbackArguments args)
		{
			var result = GetResult(ctx);
			result?.TryCompleteWithSuccess();
			return DynValue.Nil;
		}

		private DynValue CompleteWithError(ScriptExecutionContext ctx, CallbackArguments args)
		{
			var result = GetResult(ctx);
			result?.TryCompleteWithError();
			return DynValue.Nil;
		}

		private DynValue Clear(ScriptExecutionContext ctx, CallbackArguments args)
		{
			var result = GetResult(ctx);
			if (result != null)
				result.ResultContentLines.Clear();
			return DynValue.Nil;
		}
	}

	*/
}
