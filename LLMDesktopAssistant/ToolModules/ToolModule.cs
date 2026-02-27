using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.Modules;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.ToolModules
{
	/// <summary>
	/// Base class for all tool modules. It provides a method to retrieve tools for the LLM assistant.
	/// </summary>
	public abstract class ToolModule : IModule
	{
		/// <summary>
		/// Gets or sets a value indicating whether the module is enabled. Default is true.
		/// When this tool module is disabled, it will not be considered when retrieving tools for the LLM assistant.
		/// </summary>
		public bool Enabled { get; set; } = true;

		/// <summary>
		/// Retrieves all tools that can be used by the LLM assistant.
		/// </summary>
		/// <returns>A collection of tools.</returns>
		public abstract IEnumerable<ITool> GetTools();
	}
}