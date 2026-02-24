using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Quic;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.Modules;
using LLMDesktopAssistant.Utils;
using NAudio.Wave;

namespace LLMDesktopAssistant.Speech
{
	/// <summary>
	/// Class that listens for keyboard input, listens to microphone input and triggers speech events.
	/// </summary>
	[DynamicModule("KeyboardTriggerSpeechProvider", typeof(ISpeechProvider))]
	public class KeyboardTriggerSpeechProvider : ISpeechProvider
	{
		private DynamicModuleTracker<ISpeechRecognizer> _recognizer = null!;
		private List<float> accumulatedAudioData = [];
		private WaveInEvent _micStream = null!;

		public event Action<string>? OnSpeechReceived;

		public void Initialize()
		{
			_recognizer = ModuleManager.GetDynamicTracker<ISpeechRecognizer>();

			_micStream = new WaveInEvent
			{
				DeviceNumber = 0,
				WaveFormat = new WaveFormat(16000, 16, 1)
			};

			_micStream.DataAvailable += (s, e) =>
			{
				var buf = e.Buffer;
				var len = e.BytesRecorded;

				var samples = new short[len / 2];
				Buffer.BlockCopy(buf, 0, samples, 0, len);

				float[] floatSamples = new float[samples.Length];

				for (int i = 0; i < samples.Length; i++)
				{
					float normalizedSample = samples[i] / 32768f;
					floatSamples[i] = normalizedSample;
				}

				if (KeyboardUtils.IsKeyDown(System.Windows.Forms.Keys.Space))
				{
					accumulatedAudioData.AddRange(floatSamples);
				}
				else if (accumulatedAudioData.Count > 0)
				{
					accumulatedAudioData.AddRange(floatSamples);

					var audioSamples = accumulatedAudioData.ToArray();
					accumulatedAudioData.Clear();
					var recognizer = _recognizer.Module;

					Task.Run(async () =>
					{
						var result = await recognizer.RecognizeSpeechAsync(audioSamples);
						OnSpeechReceived?.Invoke(result);
					});
				}
			};

			_micStream.StartRecording();
		}

		public void Shutdown()
		{
			_micStream.StopRecording();
			_micStream.Dispose();
		}
	}
}