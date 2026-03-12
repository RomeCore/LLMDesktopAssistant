using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Quic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using LLMDesktopAssistant.Modules;
using LLMDesktopAssistant.Utils;
using NAudio.Wave;
using NHotkey;
using NHotkey.Wpf;

namespace LLMDesktopAssistant.Speech
{
	/// <summary>
	/// Class that listens for keyboard input, listens to microphone input and triggers speech events.
	/// </summary>
	[DynamicModule("KeyboardTriggerSpeechProvider", typeof(IUserSpeechProvider), IsDefault = true)]
	public class KeyboardTriggerSpeechProvider : IUserSpeechProvider
	{
		private DynamicModuleTracker<ISpeechRecognizer> _recognizer = null!;
		private List<float> accumulatedAudioData = [];
		private WaveInEvent _micStream = null!;
		private bool _registeredHotkey = false;

		public event Action<string>? OnSpeechReceived;

		public void Initialize()
		{
			_recognizer = ModuleManager.GetDynamicTracker<ISpeechRecognizer>();

			_micStream = new WaveInEvent
			{
				DeviceNumber = 0,
				WaveFormat = new WaveFormat(16000, 16, 1)
			};

			try
			{
				HotkeyManager.Current.AddOrReplace(
					"PushToTalk",
					new KeyGesture(Key.NumPad0, ModifierKeys.None),
					noRepeat: true,
					OnPushToTalk);
				_registeredHotkey = true;
			}
			catch
			{
				_registeredHotkey = false;
			}

			_micStream.DataAvailable += MicDataAvailable;
		}

		private void OnPushToTalk(object? sender, HotkeyEventArgs e)
		{
			e.Handled = true;

			_micStream.StartRecording();
		}

		private void MicDataAvailable(object? sender, WaveInEventArgs e)
		{
			if (KeyboardUtils.IsKeyDown(System.Windows.Forms.Keys.NumPad0))
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
				// If we did registered hotkey, we can stop recording for now, since the recording is triggered by hotkey manager.
				if (_registeredHotkey)
					_micStream.StopRecording();

				var audioSamples = accumulatedAudioData.ToArray();
				accumulatedAudioData.Clear();
				var recognizer = _recognizer.Module;

				if (recognizer != null)
					Task.Run(async () =>
					{
						var result = await recognizer.RecognizeSpeechAsync(audioSamples);
						OnSpeechReceived?.Invoke(result);
						Console.WriteLine(result);
					});
			}
		}

		public void Shutdown()
		{
			_micStream.StopRecording();
			_micStream.Dispose();
		}
	}
}