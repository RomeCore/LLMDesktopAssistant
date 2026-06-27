using AsyncLua.Values;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Scripting;
using Material.Icons;
using MoonSharp.Interpreter;

namespace LLMDesktopAssistant.Tools.Implementations
{
	[ToolModule]
	public class LuaInterpreterToolModule : ToolModule
	{
		private readonly LuaService _lua;

		public LuaInterpreterToolModule(LuaService lua)
		{
			_lua = lua;

			AddTool(Execute, ExecuteStreaming, ExecutePreview,
				new ToolInitializationInfo
				{
					Name = "lua-execute",
					DescriptionGetter = () => $"""
						# MAIN INFO
						Lua is executing using AsyncLua v0.2.2 (Lua 5.5).
						Executes Lua and returns the script result along with messages printed by 'print' function
						(the `dass.tool.result.write` works in a similar way).
						Lua has the API to interact with the application (called dASS) with these namespaces:
						{string.Join(", ", lua.Namespaces.Select(ns => ns != null ? $"**{ns}**" : "_G").Order())}

						# AsyncLua changes
						You can use `async/await` in your scripts, for example:
						local async function doWork()
							await delay(100)
							return 'done'
						end
						print(await doWork())

						# SMART UX WITH STREAMING AND STATUS ICONS/TITLES
						You can also use the `dass.tool.result` for streaming output, progress and status
						(for meta-tools and long-running scripts):
				
						-- 1. Basic streaming output with status icon (from Material Icons)
						dass.tool.result.set_status("Download", "Processing...") -- "Download" is the icon name, "Processing..." is the title
						dass.tool.result.write("Step 1: Starting...")
						time.sleep(100)
						dass.tool.result.write("Step 2: Working...")
						time.sleep(100)
						dass.tool.result.write("Step 3: Done!")
						dass.tool.result.complete_with_success()

						-- 2. Progress bar and Markdown output
						dass.tool.result.use_markdown(true)
						dass.tool.result.set_status("ChartTimeline", "Processing...")
						dass.tool.result.set_progress(0, 0, 10) -- current, min, max
						for i = 1, 10 do
						  dass.tool.result.set_progress(i)
						  dass.tool.result.write(string.format("  - **Item %d** completed", i))
						  time.sleep(100) -- simulate work
						end
						dass.tool.result.set_progress(1.0)
						dass.tool.result.set_status("Check", "All done!")
						dass.tool.result.complete_with_success()

						-- 3. Structured result + error handling
						local ok, data = pcall(fs.read, "data.json")
						if not ok then
						  dass.tool.result.set_status("AlertCircle", "File not found")
						  dass.tool.result.write("Error: " .. data)
						  dass.tool.result.complete_with_error()
						  return
						end
						local parsed = json.decode(data)
						dass.tool.result.set_structured(parsed)
						dass.tool.result.set_status("FileCheck", "Loaded")
						dass.tool.result.complete_with_success()
						
						# SEE MANUALS BEFORE USING THE API
						Use `manuals(...)` function to get the documentation for a specific namespace, `print(manuals(_G))`
						or `print(manuals(dass.agents, dass.tool, dass.tool.result))` for example.
						""",
					Category = "Lua",
					DefaultExpectedBehaviour = ToolBehaviour.PossiblyUnexpected
				});
		}

		public StreamingToolArgumentsAnalysisResult ExecuteStreaming(string? lua)
		{
			int lines = 0;
			if (lua != null)
				foreach (var line in lua.EnumerateLines())
					lines++;

			return new StreamingToolArgumentsAnalysisResult
			{
				StatusIcon = MaterialIconKind.LanguageLua,
				StatusTitle = LocalizationManager.LocalizeStaticFormat("lines_count", lines)
			};
		}

		public PreviewToolExecutionResult ExecutePreview(string lua)
		{
			return new PreviewToolExecutionResult
			{
				StatusIcon = MaterialIconKind.LanguageLua,
				StatusTitle = null
			};
		}

		public ReactiveToolResult Execute(
			string lua,
			ToolExecutionContext context,
			bool isolatedExecution = true)
		{
			var reactiveResult = new ReactiveToolResult
			{
				StatusIcon = MaterialIconKind.LanguageLua,
				StatusTitle = null
			};

			_ = Task.Run(async () =>
			{
				try
				{
					LuaTuple scriptResult;
					if (isolatedExecution)
					{
						scriptResult = await _lua.ExecuteAsync(lua, print => reactiveResult.ResultContentLines.Add(print), g =>
						{
							g[LuaVariables.ToolExecutionContext] = LuaValueConverter.ToLuaValue(context);
							g[LuaVariables.ToolReactiveResult] = LuaValueConverter.ToLuaValue(reactiveResult);
						});
					}
					else
					{
						scriptResult = await _lua.ExecuteAsync(lua, print => reactiveResult.ResultContentLines.Add(print), g =>
						{
							g[LuaVariables.ToolExecutionContext] = LuaValueConverter.ToLuaValue(context);
							g[LuaVariables.ToolReactiveResult] = LuaValueConverter.ToLuaValue(reactiveResult);
						});
					}

					reactiveResult.ResultContentLines.Add($"Script returned: " + scriptResult.ToString());
					reactiveResult.TryCompleteWithSuccess();
				}
				catch (ScriptRuntimeException srex)
				{
					reactiveResult.ResultContentLines.Add("Caught error: " + srex.DecoratedMessage);
					reactiveResult.ResultContentLines.Add("Remember to read the manuals for API");
					reactiveResult.TryCompleteWithError();
				}
				catch (Exception ex)
				{
					reactiveResult.ResultContentLines.Add("Caught error: " + ex.Message);
					reactiveResult.ResultContentLines.Add("Remember to read the manuals for API");
					reactiveResult.TryCompleteWithError();
				}
				finally
				{
				}
			}, CancellationToken.None);

			return reactiveResult;
		}
	}
}