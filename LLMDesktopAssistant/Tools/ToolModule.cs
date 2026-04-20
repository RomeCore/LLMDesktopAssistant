using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.Services;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.Tools
{
	/// <summary>
	/// Base class for all tool modules. It provides a method to retrieve tools for the LLM assistant.
	/// </summary>
	public abstract class ToolModule
	{
		private readonly List<ToolInfo> _tools = [];

		/// <summary>
		/// Gets the backing list of tools provided by this module.
		/// </summary>
		protected List<ToolInfo> Tools => _tools;

		/// <summary>
		/// Gets or sets a value indicating whether the module is enabled. Default is true.
		/// When this tool module is disabled, it will not be considered when retrieving tools for the LLM assistant.
		/// </summary>
		public bool Enabled { get; set; } = true;

		/// <summary>
		/// Adds a tool to this module.
		/// </summary>
		/// <param name="tool">The tool to add.</param>
		protected void AddTool(ToolInfo tool)
		{
			_tools.Add(tool);
		}

		/// <summary>
		/// Adds a tool to this module.
		/// </summary>
		/// <param name="executor">The delegate that will execute the tool.</param>
		/// <param name="info">The initialization information for the tool.</param>
		protected void AddTool(Delegate executor, ToolInitializationInfo info)
		{
			_tools.Add(ToolInfo.Create(executor, info));
		}

		/// <summary>
		/// Removes all tools from this module.
		/// </summary>
		protected void ClearTools()
		{
			_tools.Clear();
		}

		/// <summary>
		/// Replaces the current collection of tools with the specified sequence.
		/// </summary>
		/// <param name="tools">The sequence of tools to set as the new collection. Cannot be null.</param>
		protected void ReplaceTools(IEnumerable<ToolInfo> tools)
		{
			_tools.Clear();
			_tools.AddRange(tools);
		}

		/// <summary>
		/// Retrieves all tools that can be used by the LLM assistant.
		/// </summary>
		/// <returns>A collection of tools.</returns>
		public virtual IEnumerable<ToolInfo> GetTools()
		{
			return _tools;
		}
	}
}