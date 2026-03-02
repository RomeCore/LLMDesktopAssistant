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
	[DynamicModule("KeyboardTriggerSpeechProvider", typeof(IUserSpeechProvider))]
	public class KeyboardTriggerSpeechProvider : IUserSpeechProvider
	{
		private DynamicModuleTracker<IUserSpeechRecognizer> _recognizer = null!;
		private List<float> accumulatedAudioData = [];
		private WaveInEvent _micStream = null!;

		public event Action<string>? OnSpeechReceived;

		public void Initialize()
		{
			_recognizer = ModuleManager.GetDynamicTracker<IUserSpeechRecognizer>();

			_micStream = new WaveInEvent
			{
				DeviceNumber = 0,
				WaveFormat = new WaveFormat(16000, 16, 1)
			};

			_micStream.DataAvailable += (s, e) =>
			{
				if (KeyboardUtils.IsKeyDown(System.Windows.Forms.Keys.Space))
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

					accumulatedAudioData.AddRange(floatSamples);
				}
				else if (accumulatedAudioData.Count > 0)
				{
					var audioSamples = accumulatedAudioData.ToArray();
					accumulatedAudioData.Clear();
					var recognizer = _recognizer.Module;

					if (recognizer != null)
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