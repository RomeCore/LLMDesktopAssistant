using System;
using System.Collections.Generic;
using System.Text;
using LLMDesktopAssistant.Scripting.Lua;

namespace LLMDesktopAssistant.Scripting
{
	/// <summary>
	/// Provides a way to manage and provide Lua scripts for execution within an application.
	/// </summary>
	public interface ILuaUserScriptManager
	{
		/// <summary>
		/// Occurs when the scripts have been changed.
		/// </summary>
		event EventHandler? ScriptsChanged;

		/// <summary>
		/// Returns a list of Lua API objects that can be used to register them into the Lua environment.
		/// </summary>
		/// <returns>A collection of Lua API objects.</returns>
		IEnumerable<LuaApiLoadedScript> GetScripts();

		/// <summary>
		/// Registers a Lua script with the manager.
		/// </summary>
		/// <param name="path">The file path of the Lua script to register.</param>
		/// <param name="ns">The namespace under which the script should be registered.</param>
		/// <param name="manuals">A string containing documentation or manual for the script.</param>
		/// <param name="script">The Lua script content as a string.</param>
		void RegisterOrUpdateScript(string path, string? ns, string? manuals, string? script);

		/// <summary>
		/// Removes a Lua script from the manager.
		/// </summary>
		/// <param name="path">The file path of the Lua script to remove.</param>
		bool RemoveScript(string path);

		/// <summary>
		/// Moves a Lua script from one path to another.
		/// </summary>
		/// <param name="oldPath">The current file path of the Lua script.</param>
		/// <param name="newPath">The new file path where the Lua script should be moved to.</param>
		bool MoveScript(string oldPath, string newPath);
	}
}