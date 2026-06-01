using System;
using System.Collections.Generic;
using System.Text;
using LLTSharp;

namespace LLMDesktopAssistant.Prompting.Plugins
{
	/// <summary>
	/// Interface for plugins that provide additional functionality to prompt templates.
	/// </summary>
	public interface IPromptTemplatePlugin
	{
		/// <summary>
		/// Returns a collection of template functions that can be used inside the prompt templates.
		/// </summary>
		/// <returns>A collection of template functions.</returns>
		IEnumerable<TemplateFunction> GetTemplateFunctions();
	}
}
