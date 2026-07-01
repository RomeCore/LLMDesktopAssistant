using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Providers
{
	public enum ModelProviderType
	{
		// ==============================
		// OpenAI-compatible group
		// ==============================

		/// <summary>
		/// A model provider that is compatible with OpenAI's style API.
		/// </summary>
		OpenAICompatible,

		/// <summary>
		/// An OpenAI model provider (OpenAI-compatible).
		/// </summary>
		OpenAI,

		/// <summary>
		/// An OpenRouter model provider (OpenAI-compatible).
		/// </summary>
		OpenRouter,

		/// <summary>
		/// A DeepSeek model provider (OpenAI-compatible).
		/// </summary>
		DeepSeek,

		/// <summary>
		/// A Novita model provider (OpenAI-compatible).
		/// </summary>
		Novita,

		// ==============================
		// Other providers
		// ==============================

		/// <summary>
		/// An Ollama model provider (specific).
		/// </summary>
		Ollama
	}
}
