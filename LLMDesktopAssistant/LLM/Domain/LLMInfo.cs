using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.Tools;
using RCLargeLanguageModels;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.LLM.Domain
{
	/// <summary>
	/// Represents information about the Large Language Model (LLM) being used.
	/// </summary>
	public class LLMInfo
	{
		/// <summary>
		/// Gets or sets the language model used by the assistant.
		/// </summary>
		public required LLModel LLM { get; init; }
	}
}