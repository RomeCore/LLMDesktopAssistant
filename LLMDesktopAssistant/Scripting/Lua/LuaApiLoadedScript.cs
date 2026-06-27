using AsyncLua.Values;
using MoonSharp.Interpreter;

namespace LLMDesktopAssistant.Scripting.Lua
{
	public class LuaApiLoadedScript : LuaApiBaseAsync
	{
		public string? Path { get; }
		public override string? Namespace { get; }
		public override string? Manuals { get; }
		public string Script { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="LuaApiLoadedScript"/> class.
		/// </summary>
		/// <param name="path">The path to the file containing the Lua script.</param>
		/// <param name="namespace">The namespace of the API. This is used to organize functions and properties in Lua.</param>
		/// <param name="manuals">The manuals for the API. These are used to provide documentation and help in LLM.</param>
		/// <param name="script">The Lua script to execute. This should define the functions and properties for this API.</param>
		public LuaApiLoadedScript(string? path, string? @namespace, string? manuals, string script)
		{
			Path = path;
			Namespace = @namespace;
			Manuals = manuals;
			Script = script;
		}

		public override void Populate(LuaTable globals, LuaTable ns, LuaService luaService)
		{
			globals["_NS"] = ns;
			try
			{
				luaService.Execute(Script);
			}
			finally
			{
				globals.Remove("_NS");
			}
		}
	}
}
