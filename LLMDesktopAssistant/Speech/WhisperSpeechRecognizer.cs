using System.IO;
using System.Text;
using LLMDesktopAssistant.Modules;
using Whisper.net;

namespace LLMDesktopAssistant.Speech
{
	/// <summary>
	/// Class that uses the Whisper speech recognition model to recognize speech.
	/// </summary>
	[DynamicModule("WhisperSpeechRecognizer", typeof(ISpeechRecognizer))]
	public class WhisperSpeechRecognizer : ISpeechRecognizer
	{
		WhisperFactory _factory = null!;
		WhisperProcessor _processor = null!;

		public void Initialize()
		{
			var modelFileName = "models/whisper/ggml-medium.bin";

			if (!File.Exists(modelFileName))
			{
				Console.WriteLine("Model file not found. Please ensure that the model file is present in the current directory.");
				return;
			}

			_factory = WhisperFactory.FromPath(modelFileName);
			_processor = _factory
				.CreateBuilder()
				.WithLanguage("ru")
				.WithThreads(4)
				.Build();
		}

		public async Task<string> RecognizeSpeechAsync(float[] samples)
		{
			StringBuilder result = new();
			await foreach (var segment in _processor.ProcessAsync(samples))
				result.Append(segment.Text);
			return result.ToString();
		}

		public void Shutdown()
		{
			_factory.Dispose();
			_processor.Dispose();
		}
	}
}