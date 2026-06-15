using System.Text;
using System.Xml.Linq;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Scripting.Lua;
using MoonSharp.Interpreter;
using Serilog;

namespace LLMDesktopAssistant.Scripting
{
	[ChatService]
	public class LuaService
	{
		private readonly Script _lua;
		private readonly List<string?> _namespaces;
		private readonly Table _globalTableSnapshot;
		private readonly List<string?> _namespacesSnapshot;
		private readonly ILuaUserScriptManager _scriptManager;

		/// <summary>
		/// Gets the Lua interpreter instance.
		/// </summary>
		public Script Lua => _lua;

		/// <summary>
		/// Gets the list of namespaces available in Lua.
		/// </summary>
		public IReadOnlyList<string?> Namespaces { get; }

		public LuaService(IEnumerable<LuaApiBase> apis, ILuaUserScriptManager scriptManager)
		{
			_lua = new Script(CoreModules.Preset_SoftSandbox & ~(CoreModules.Dynamic | CoreModules.Json));
			_lua.Options.CheckThreadAccess = false;
			_namespaces = [ null ];
			_scriptManager = scriptManager;
			Namespaces = _namespaces.AsReadOnly();

			_lua.Globals.Set("_ns_api", DynValue.NewBoolean(true));
			_lua.Globals.Set("_ns_part", DynValue.NewString("_G"));
			_lua.Globals.Set("_ns_path", DynValue.NewString("_G"));

			foreach (var api in apis)
				RegisterApi(api);

			_globalTableSnapshot = _lua.Globals.DeepClone();
			_namespacesSnapshot = [.. _namespaces];
			RefreshUserScripts();

			_scriptManager.ScriptsChanged += (s, e) =>
			{
				ResetGlobalTable();
				RefreshUserScripts();
			};
		}

		private void RegisterApi(LuaApiBase api)
		{
			var globals = _lua.Globals;
			var ns = api.Namespace != null ? ResolveNamespace(api.Namespace) : globals;
			api.Populate(globals, ns, this);

			var manuals = ns.Get("_manuals");
			if (manuals.Type != DataType.Table)
			{
				manuals = DynValue.NewTable(_lua);
				ns.Set("_manuals", manuals);
			}
			var apiManuals = api.Manuals;
			if (apiManuals != null)
				manuals.Table.Append(DynValue.NewString(apiManuals));
		}

		private void ResetGlobalTable()
		{
			_lua.Globals.Clear();
			foreach (var kvp in _globalTableSnapshot.Pairs)
			{
				var key = kvp.Key;
				var value = kvp.Value;
				_lua.Globals.Set(key, value);
			}
			_namespaces.Clear();
			_namespaces.AddRange(_namespacesSnapshot);
		}

		private void RefreshUserScripts()
		{
			try
			{
				var scripts = _scriptManager.GetScripts();

				foreach (var script in scripts)
				{
					try
					{
						RegisterApi(script);
					}
					catch (Exception ex)
					{
						Log.Error(ex, "Failed to execute user script: {ScriptPath} (namespace: {Namespace}), Error: {ErrorMessage}",
							script.Path, script.Namespace ?? "*global namespace*", ex.Message);
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to refresh user scripts: {Error}", ex.Message);
			}
		}

		/// <summary>
		/// Resolves a Lua namespace to a table if it exists.
		/// </summary>
		/// <param name="namespaceName">The Lua namespace string to resolve.</param>
		/// <returns>The resolved Lua table, or null if the namespace does not exist.</returns>
		public Table? TryResolveNamespace(string namespaceName)
		{
			var parts = namespaceName.Split(['.'], StringSplitOptions.RemoveEmptyEntries);
			var result = _lua.Globals;

			foreach (var part in parts)
			{
				var next = result.Get(part);
				if (next.Type != DataType.Table)
					return null;
				result = next.Table;
			}

			return result;
		}

		/// <summary>
		/// Resolves a Lua namespace to a table.
		/// </summary>
		/// <param name="namespaceName">The Lua namespace string to resolve.</param>
		/// <returns>The resolved Lua table.</returns>
		public Table ResolveNamespace(string namespaceName)
		{
			var parts = namespaceName.Split(['.'], StringSplitOptions.RemoveEmptyEntries);
			var result = _lua.Globals;

			var accumulatedPath = new StringBuilder();
			foreach (var part in parts)
			{
				var next = result.Get(part);

				if (accumulatedPath.Length > 0)
					accumulatedPath.Append('.');
				accumulatedPath.Append(part);

				if (next.Type != DataType.Table)
				{
					next = DynValue.NewTable(_lua);

					var table = next.Table;
					_namespaces.Add(accumulatedPath.ToString());
					table.Set("_ns_api", DynValue.NewBoolean(true));
					table.Set("_ns_part", DynValue.NewString(part));
					table.Set("_ns_path", DynValue.NewString(accumulatedPath.ToString()));

					result.Set(part, next);
				}
				else if (!next.Table.Get("_ns_api").CastToBool())
				{
					var table = next.Table;
					_namespaces.Add(accumulatedPath.ToString());
					table.Set("_ns_api", DynValue.NewBoolean(true));
					table.Set("_ns_part", DynValue.NewString(part));
					table.Set("_ns_path", DynValue.NewString(accumulatedPath.ToString()));
				}

				result = next.Table;
			}

			return result;
		}

		/// <summary>
		/// Executes the provided Lua code.
		/// </summary>
		/// <param name="lua">The Lua code to execute.</param>
		/// <param name="modifyGlobals">Action used to modify cloned _G table. If not null, the globals will be cloned and passed to this action, then passed to Lua.</param>
		/// <returns>The result of the Lua execution.</returns>
		public DynValue Execute(string lua, Action<Table>? modifyGlobals = null)
		{
			try
			{
				var globals = _lua.Globals;
				if (modifyGlobals != null)
				{
					globals = globals.ShallowClone();
					modifyGlobals(globals);
				}
				return _lua.DoString(lua, globals);
			}
			finally
			{
			}
		}

		/// <summary>
		/// Executes the provided Lua code and captures any output generated by print statements.
		/// </summary>
		/// <param name="lua">The Lua code to execute.</param>
		/// <param name="printOutput">The list to capture output into.</param>
		/// <param name="modifyGlobals">Action used to modify cloned _G table. If not null, the globals will be cloned and passed to this action, then passed to Lua.</param>
		/// <returns>The result of the Lua execution.</returns>
		public DynValue Execute(string lua, Action<string> printOutput, Action<Table>? modifyGlobals = null)
		{
			var prevPrint = _lua.Options.DebugPrint;
			try
			{
				_lua.Options.DebugPrint = printOutput;
				var globals = _lua.Globals;
				if (modifyGlobals != null)
				{
					globals = globals.ShallowClone();
					modifyGlobals(globals);
				}
				return _lua.DoString(lua, globals);
			}
			finally
			{
				_lua.Options.DebugPrint = prevPrint;
			}
		}

		/// <summary>
		/// Executes the provided Lua code and captures any output generated by print statements.
		/// </summary>
		/// <param name="lua">The Lua code to execute.</param>
		/// <param name="printOutput">The list to capture output into.</param>
		/// <param name="modifyGlobals">Action used to modify cloned _G table. If not null, the globals will be cloned and passed to this action, then passed to Lua.</param>
		/// <returns>The result of the Lua execution.</returns>
		public DynValue Execute(string lua, List<string> printOutput, Action<Table>? modifyGlobals = null)
		{
			var prevPrint = _lua.Options.DebugPrint;
			try
			{
				_lua.Options.DebugPrint = str => printOutput.Add(str);
				var globals = _lua.Globals;
				if (modifyGlobals != null)
				{
					globals = globals.ShallowClone();
					modifyGlobals(globals);
				}
				return _lua.DoString(lua, globals);
			}
			finally
			{
				_lua.Options.DebugPrint = prevPrint;
			}
		}

		/// <summary>
		/// Executes the provided Lua code and captures any output generated by print statements.
		/// </summary>
		/// <param name="lua">The Lua code to execute.</param>
		/// <param name="printOutput">The list to capture output into.</param>
		/// <param name="modifyGlobals">Action used to modify cloned _G table. If not null, the globals will be cloned and passed to this action, then passed to Lua.</param>
		/// <returns>The result of the Lua execution.</returns>
		public DynValue Execute(string lua, out List<string> printOutput, Action<Table>? modifyGlobals = null)
		{
			var _printOutput = new List<string>();
			printOutput = _printOutput;

			var prevPrint = _lua.Options.DebugPrint;
			try
			{
				_lua.Options.DebugPrint = str => _printOutput.Add(str);
				var globals = _lua.Globals;
				if (modifyGlobals != null)
				{
					globals = globals.ShallowClone();
					modifyGlobals(globals);
				}
				return _lua.DoString(lua, globals);
			}
			finally
			{
				_lua.Options.DebugPrint = prevPrint;
			}
		}
	}
}