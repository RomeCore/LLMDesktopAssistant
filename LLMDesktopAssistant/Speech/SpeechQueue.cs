using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.Modules;
using Serilog;

namespace LLMDesktopAssistant.Speech
{
	/// <summary>
	/// Class that manages a queue of strings for speech processing.
	/// </summary>
	public static class SpeechQueue
	{
		private static readonly ConcurrentQueue<string> _queue = new();
		private static readonly SemaphoreSlim _signal = new(0);
		private static CancellationTokenSource _speechCts = new();

		static SpeechQueue()
		{
			_ = Task.Run(ProcessQueue);
		}

		/// <summary>
		/// Adds a string to the queue for processing.
		/// </summary>
		/// <param name="speech">The string to add to the queue.</param>
		public static void Enqueue(string speech)
		{
			_queue.Enqueue(speech);
			Console.WriteLine(speech);
			_signal.Release();
		}

		/// <summary>
		/// Cancels the current speech processing task and resets the cancellation token source.
		/// </summary>
		public static void CancelCurrent()
		{
			_speechCts.Cancel();
			_speechCts = new CancellationTokenSource();
		}

		/// <summary>
		/// Clears all items from the queue, causing to prevent playing any further queued speech.
		/// </summary>
		public static void Clear()
		{
			while (_queue.TryDequeue(out _)) { }
		}

		/// <summary>
		/// Cancels the current speech processing task and clears all items from the queue.
		/// </summary>
		public static void CancelAll()
		{
			CancelCurrent();
			Clear();
		}

		private static async Task ProcessQueue()
		{
			while (true)
			{
				await _signal.WaitAsync();

				if (_queue.TryDequeue(out var speech))
				{
					try
					{
						var module = ModuleManager.GetDynamic<IAssistantSpeechPlayer>();

						await module.SpeakAsync(
							speech,
							_speechCts.Token);
					}
					catch (OperationCanceledException)
					{
					}
					catch (Exception ex)
					{
						Log.Error(ex, "Failed to process speech: {Speech}", speech);
					}
				}
			}
		}
	}
}