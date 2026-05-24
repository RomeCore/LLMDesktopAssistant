using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Scripting
{
	/// <summary>
	/// Represents different types of script languages that can be used in the application.
	/// </summary>
	public enum ScriptLanguageType
	{
		/// <summary>
		/// Unknown language type. This is the default value for an uninitialized variable or when no valid language is detected.
		/// </summary>
		Unknown,

		/// <summary>
		/// Lua scripting language.
		/// </summary>
		Lua,

		/// <summary>
		/// Python scripting language.
		/// </summary>
		Python
	}
}