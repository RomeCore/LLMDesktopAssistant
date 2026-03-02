using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.Modules;

namespace LLMDesktopAssistant.Speech
{
	/// <summary>
	/// Interface for user speech provider.
	/// </summary>
	public interface IUserSpeechProvider : IDynamicModule
	{
		/// <summary>
		/// Event triggered when speech is received, e.g. from a microphone or other input device.
		/// </summary>
		public event Action<string>? OnSpeechReceived;
	}
}