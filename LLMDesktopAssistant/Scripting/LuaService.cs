using System.Text;
using System.Xml.Linq;
using AsyncLua;
using AsyncLua.Values;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Scripting.Lua;
using MoonSharp.Interpreter;
using Serilog;

namespace LLMDesktopAssistant.Scripting
{
	[ChatService]
	public class LuaService
	{
		private readonly LuaState _lua;
		private readonly List<string?> _namespaces;

		private readonly LuaTable _globalTableSnapshot;
		private readonly Dictionary<LuaType, LuaMetatable> _typeMetatablesSnapshot;
		private readonly List<string?> _namespacesSnapshot;
		private readonly ILuaUserScriptManager _scriptManager;

		/// <summary>
		/// Gets the list of namespaces available in Lua.
		/// </summary>
		public IReadOnlyList<string?> Namespaces { get; }

		public LuaService(IEnumerable<LuaApiBaseAsync> apis, ILuaUserScriptManager scriptManager)
		{
			_lua = new LuaState().LoadDefaultLibraries();
			_namespaces = [ null ];
			_scriptManager = scriptManager;
			Namespaces = _namespaces.AsReadOnly();

			_lua.Globals.Set(LuaVariables.NamespaceApiMarker, LuaBoolean.True);
			_lua.Globals.Set(LuaVariables.NamespacePartPath, new LuaString(LuaVariables.GlobalTable));
			_lua.Globals.Set(LuaVariables.NamespaceFullPath, new LuaString(LuaVariables.GlobalTable));

			foreach (var api in apis)
				RegisterApi(api);

			_globalTableSnapshot = _lua.Globals.DeepClone();
			_namespacesSnapshot = [.. _namespaces];
			_typeMetatablesSnapshot = _lua.TypeMetatables.ToDictionary();
			RefreshUserScripts();

			_scriptManager.ScriptsChanged += (s, e) =>
			{
				ResetGlobalTable();
				RefreshUserScripts();
			};
		}

		private void RegisterApi(LuaApiBaseAsync api)
		{
			var globals = _lua.Globals;
			var ns = api.Namespace != null ? ResolveNamespace(api.Namespace) : globals;
			api.Populate(globals, ns, this);

			var manuals = ns.Get(LuaVariables.NamespaceManuals);
			if (manuals is not LuaTable manualsTable)
			{
				manualsTable = new LuaTable();
				ns.Set(LuaVariables.NamespaceManuals, manualsTable);
			}
			var apiManuals = api.Manuals;
			if (apiManuals != null)
				manualsTable.Append(new LuaString(apiManuals));
		}

		private void ResetGlobalTable()
		{
			_lua.Globals.Clear();
			foreach (var kvp in _globalTableSnapshot.Entries)
			{
				var key = kvp.Key;
				var value = kvp.Value;
				_lua.Globals.Set(key, value);
			}
			_lua.TypeMetatables.Clear();
			foreach (var kvp in _typeMetatablesSnapshot)
			{
				_lua.TypeMetatables[kvp.Key] = kvp.Value.DeepClone();
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
							script.Path, script.Namespace ?? LuaVariables.GlobalTable, ex.Message);
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
		public LuaTable? TryResolveNamespace(string namespaceName)
		{
			var parts = namespaceName.Split(['.'], StringSplitOptions.RemoveEmptyEntries);
			var result = _lua.Globals;

			foreach (var part in parts)
			{
				var next = result.Get(part);
				if (next is not LuaTable nextTable)
					return null;
				result = nextTable;
			}

			return result;
		}

		/// <summary>
		/// Resolves a Lua namespace to a table.
		/// </summary>
		/// <param name="namespaceName">The Lua namespace string to resolve.</param>
		/// <returns>The resolved Lua table.</returns>
		public LuaTable ResolveNamespace(string namespaceName)
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

				if (next is not LuaTable table)
				{
					table = new LuaTable();
					_namespaces.Add(accumulatedPath.ToString());
					table.Set(LuaVariables.NamespaceApiMarker, LuaBoolean.True);
					table.Set(LuaVariables.NamespacePartPath, new LuaString(part));
					table.Set(LuaVariables.NamespaceFullPath, new LuaString(accumulatedPath.ToString()));

					result.Set(part, next);
				}
				else if (!table.Get(LuaVariables.NamespaceApiMarker).ToBoolean())
				{
					_namespaces.Add(accumulatedPath.ToString());
					table.Set(LuaVariables.NamespaceApiMarker, LuaBoolean.True);
					table.Set(LuaVariables.NamespacePartPath, new LuaString(part));
					table.Set(LuaVariables.NamespaceFullPath, new LuaString(accumulatedPath.ToString()));
				}

				result = table;
			}

			return result;
		}

		/// <summary>
		/// Creates a snapshot of the current Lua runtime.
		/// Used for running concurrent scripts without interfering with each other.
		/// </summary>
		/// <returns>A new Lua runtime with a copy of the current global table.</returns>
		public LuaState CreateSnapshotRuntime()
		{
			return _lua.CreateSnapshot();
		}

		/// <summary>
		/// Executes the provided Lua code.
		/// </summary>
		/// <param name="lua">The Lua code to execute.</param>
		/// <param name="modifyGlobals">Action used to modify cloned _G table. If not null, the globals will be cloned and passed to this action, then passed to Lua.</param>
		/// <returns>The result of the Lua execution.</returns>
		public LuaTuple Execute(string lua, Action<LuaTable>? modifyGlobals = null)
		{
			var globals = _lua.Globals;
			if (modifyGlobals != null)
			{
				globals = globals.ShallowClone();
				modifyGlobals(globals);
			}
			return _lua.Execute(lua, editContext: ctx =>
			{
				ctx.Globals = globals;
			});
		}

		/// <summary>
		/// Executes the provided Lua code and captures any output generated by print statements.
		/// </summary>
		/// <param name="lua">The Lua code to execute.</param>
		/// <param name="printOutput">The list to capture output into.</param>
		/// <param name="modifyGlobals">Action used to modify cloned _G table. If not null, the globals will be cloned and passed to this action, then passed to Lua.</param>
		/// <returns>The result of the Lua execution.</returns>
		public LuaTuple Execute(string lua, Action<string> printOutput, Action<LuaTable>? modifyGlobals = null)
		{
			var globals = _lua.Globals;
			if (modifyGlobals != null)
			{
				globals = globals.ShallowClone();
				modifyGlobals(globals);
			}
			return _lua.Execute(lua, editContext: ctx =>
			{
				ctx.Globals = globals;
				ctx.Print = printOutput;
			});
		}

		/// <summary>
		/// Executes the provided Lua code.
		/// </summary>
		/// <param name="lua">The Lua code to execute.</param>
		/// <param name="modifyGlobals">Action used to modify cloned _G table. If not null, the globals will be cloned and passed to this action, then passed to Lua.</param>
		/// <returns>The result of the Lua execution.</returns>
		public Task<LuaTuple> ExecuteAsync(string lua, Action<LuaTable>? modifyGlobals = null)
		{
			var globals = _lua.Globals;
			if (modifyGlobals != null)
			{
				globals = globals.ShallowClone();
				modifyGlobals(globals);
			}
			return _lua.ExecuteAsync(lua, editContext: ctx =>
			{
				ctx.Globals = globals;
			});
		}

		/// <summary>
		/// Executes the provided Lua code and captures any output generated by print statements.
		/// </summary>
		/// <param name="lua">The Lua code to execute.</param>
		/// <param name="printOutput">The list to capture output into.</param>
		/// <param name="modifyGlobals">Action used to modify cloned _G table. If not null, the globals will be cloned and passed to this action, then passed to Lua.</param>
		/// <returns>The result of the Lua execution.</returns>
		public Task<LuaTuple> ExecuteAsync(string lua, Action<string> printOutput, Action<LuaTable>? modifyGlobals = null)
		{
			var globals = _lua.Globals;
			if (modifyGlobals != null)
			{
				globals = globals.ShallowClone();
				modifyGlobals(globals);
			}
			return _lua.ExecuteAsync(lua, editContext: ctx =>
			{
				ctx.Globals = globals;
				ctx.Print = printOutput;
			});
		}
	}
}