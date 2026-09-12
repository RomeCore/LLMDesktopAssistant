using AsyncLua.Values;
using LLMDesktopAssistant.Addons;

namespace LLMDesktopAssistant.Scripting.Lua
{
	public class LuaScriptInfo : AddonChangedBase<LuaScriptInfo, LuaScriptChange>
	{
		/// <summary>
		/// The namespace for the Lua script. Used for registering functions and variables via '_NS' table.
		/// If <see langword="null"/>, no namespace is used. If empty, the global namespace is used.
		/// </summary>
		public string? Namespace
		{
			get;
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// The human and agent-readable manuals to put in the <see cref="Namespace"/> table and associated with this script.
		/// Will take no effect if <see cref="Namespace"/> is <see langword="null"/>.
		/// </summary>
		public string? Manuals
		{
			get;
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// Whether the script is a native script, meaning it is not provided by script file.
		/// </summary>
		public bool IsNative
		{
			get;
			set => SetProperty(ref field, value);
		}

		public Func<LuaTable, LuaTable?, LuaService, Action?> Loader
		{
			get;
			set => SetProperty(ref field, value);
		} = (_, _, _) => null;
	}
}
