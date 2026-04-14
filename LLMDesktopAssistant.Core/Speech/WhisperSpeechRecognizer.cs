using System.IO;
using System.Text;
using LLMDesktopAssistant.Core.Services;
using Whisper.net;
using Whisper.net.Ggml;

namespace LLMDesktopAssistant.Core.Speech
{
	/// <summary>
	/// Class that uses the Whisper speech recognition model to recognize speech.
	/// </summary>
	[DynamicService("WhisperSpeechRecognizer", typeof(ISpeechRecognizer), IsDefault = true)]
	public class WhisperSpeechRecognizer : ISpeechRecognizer
	{
		WhisperFactory? _factory = null;
		WhisperProcessor? _processor = null;

		public void Initialize()
		{
			var modelFileName = "models/whisper/ggml-base.bin";

			Directory.CreateDirectory("models");
			Directory.CreateDirectory("models/whisper");

			if (!File.Exists(modelFileName))
			{
				Task.Run(async () =>
				{
					Console.WriteLine($"Model file '{modelFileName}' not found. Downloading...");

					var ggmlType = GetModelType(modelFileName);
					using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(ggmlType);
					using (var fileWriter = File.OpenWrite(modelFileName))
						await modelStream.CopyToAsync(fileWriter);

					Console.WriteLine($"Model file '{modelFileName}' downloaded!");

					CreateModel(modelFileName);
				});

				return;
			}

			CreateModel(modelFileName);
		}

		private static GgmlType GetModelType(string modelFileName)
		{
			var filename = Path.GetFileNameWithoutExtension(modelFileName);

			return filename switch
			{
				"ggml-tiny" => GgmlType.Tiny,
				"ggml-tiny.en" => GgmlType.TinyEn,
				"ggml-small" => GgmlType.Small,
				"ggml-small.en" => GgmlType.SmallEn,
				"ggml-base" => GgmlType.Base,
				"ggml-base.en" => GgmlType.BaseEn,
				"ggml-medium" => GgmlType.Medium,
				"ggml-medium.en" => GgmlType.MediumEn,
				"ggml-large-v1" => GgmlType.LargeV1,
				"ggml-large-v2" => GgmlType.LargeV2,
				"ggml-large-v3" => GgmlType.LargeV3,
				"ggml-large-v3-turbo" => GgmlType.LargeV3Turbo,
				_ => GgmlType.Base
			};
		}

		private void CreateModel(string modelFileName)
		{
			_factory = WhisperFactory.FromPath(modelFileName);
			_processor = _factory
				.CreateBuilder()
				.WithLanguage("ru")
				.WithThreads(4)
				.Build();
		}

		public async Task<string> RecognizeSpeechAsync(float[] samples)
		{
			if (_processor == null)
				return string.Empty;

			StringBuilder result = new();
			await foreach (var segment in _processor.ProcessAsync(samples))
				result.Append(segment.Text);
			return result.ToString();
		}

		public void Shutdown()
		{
			_factory?.Dispose();
			_processor?.Dispose();
		}
	}
}