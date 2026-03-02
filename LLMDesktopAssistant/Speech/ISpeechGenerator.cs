using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.Modules;

namespace LLMDesktopAssistant.Speech
{
	/// <summary>
	/// Interface for speech generation from text.
	/// </summary>
	public interface ISpeechGenerator : IDynamicModule
	{
		/// <summary>
		/// Generates speech from the given text.
		/// </summary>
		/// <param name="text">The text to generate speech for.</param>
		/// <returns>Generated audio samples.</returns>
		public float[] GenerateSpeech(string text);
	}
}