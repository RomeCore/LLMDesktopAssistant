using LLMDesktopAssistant.Tools;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.LLM.Services.Tools
{
	/// <summary>
	/// Interface for building toolsets that can be used by the language model.
	/// </summary>
	public interface IToolsetBuildingService
	{
		/// <summary>
		/// Builds a collection of tools based on the current configuration.
		/// </summary>
		/// <remarks>
		/// These tools will be used directly for the LLM execution.
		/// </remarks>
		/// <returns>A collection of <see cref="ToolInfo"/> objects representing the tools to be used by the language model.</returns>
		IEnumerable<ToolInfo> BuildTools();

		/// <summary>
		/// Returns a collection of tools that are available for selection but not necessarily active in the current configuration.
		/// </summary>
		/// <remarks>
		/// These tools can be displayed in a UI for users to choose which ones to enable or disable.
		/// </remarks>
		/// <returns>A collection of <see cref="ToolInfo"/> objects representing the available tools.</returns>
		IEnumerable<ToolInfo> GetAvailableTools();
	}
}