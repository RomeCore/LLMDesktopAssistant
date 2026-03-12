using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.Modules;

namespace LLMDesktopAssistant.Speech
{
	[DynamicModule("WindowsSystemSpeechPlayer", typeof(IAssistantSpeechPlayer), IsDefault = true)]
	public class WindowsSystemSpeechPlayer : IAssistantSpeechPlayer
	{
		private readonly SpeechSynthesizer _synthesizer;
		private TaskCompletionSource? _tcs;

		public WindowsSystemSpeechPlayer()
		{
			_synthesizer = new SpeechSynthesizer();
			_synthesizer.Rate = 2;
			_synthesizer.Volume = 60;
			_synthesizer.SelectVoice("Microsoft Pavel");

			_synthesizer.SpeakCompleted += SpeakCompleted;
		}

		private void SpeakCompleted(object? sender, SpeakCompletedEventArgs e)
		{
			_tcs?.TrySetResult();
			_tcs = null;
		}

		public async Task SpeakAsync(string text, CancellationToken cancellationToken)
		{
			if (_tcs != null)
			{
				_synthesizer.SpeakAsyncCancelAll();
				_tcs.TrySetResult();
			}

			_tcs = new TaskCompletionSource();
			Prompt? currentPrompt = null;

			using (cancellationToken.Register(() =>
			{
				_synthesizer.SpeakAsyncCancel(currentPrompt);
			}))
			{
				currentPrompt = _synthesizer.SpeakAsync(text);
				await _tcs.Task;
			}
		}
	}
}