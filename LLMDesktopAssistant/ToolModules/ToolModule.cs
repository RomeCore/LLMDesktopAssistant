using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.ToolModules
{
	/// <summary>
	/// Base class for all tool modules. It provides a method to retrieve tools for the LLM assistant.
	/// </summary>
	public abstract class ToolModule
	{
		/// <summary>
		/// Retrieves all tools that can be used by the LLM assistant.
		/// </summary>
		/// <returns>A collection of tools.</returns>
		public abstract IEnumerable<ITool> GetTools();
	}
}