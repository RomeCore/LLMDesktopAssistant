using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Prompting.ContextExpanders
{
	/// <summary>
	/// Interface for expanding the system prompt context for rendering prompt templates.
	/// Used for system prompt and prompt components (components, behaviour sliders, personas, specializations).
	/// </summary>
	public interface IPromptSystemContextExpander
	{
		/// <summary>
		/// Expands the prompt context with additional information.
		/// </summary>
		/// <param name="context">The current prompt context as a dictionary.</param>
		void ExpandPromptContext(Dictionary<string, object?> context);
	}
}