using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.Services;
using Serilog;

namespace LLMDesktopAssistant.Speech
{
	/// <summary>
	/// Class that manages a queue of strings for speech processing.
	/// </summary>
	// [Service]
	public class SpeechQueue
	{
		private readonly ConcurrentQueue<string> _queue = new();
		private readonly SemaphoreSlim _signal = new(0);
		private CancellationTokenSource _speechCts = new();
		private readonly IAssistantSpeechPlayer _player;

		public SpeechQueue(IAssistantSpeechPlayer player)
		{
			_player = player;
			_ = Task.Run(ProcessQueue);
		}

		/// <summary>
		/// Adds a string to the queue for processing.
		/// </summary>
		/// <param name="speech">The string to add to the queue.</param>
		public void Enqueue(string speech)
		{
			_queue.Enqueue(speech);
			Console.WriteLine(speech);
			_signal.Release();
		}

		/// <summary>
		/// Cancels the current speech processing task and resets the cancellation token source.
		/// </summary>
		public void CancelCurrent()
		{
			_speechCts.Cancel();
			_speechCts = new CancellationTokenSource();
		}

		/// <summary>
		/// Clears all items from the queue, causing to prevent playing any further queued speech.
		/// </summary>
		public void Clear()
		{
			while (_queue.TryDequeue(out _)) { }
		}

		/// <summary>
		/// Cancels the current speech processing task and clears all items from the queue.
		/// </summary>
		public void CancelAll()
		{
			CancelCurrent();
			Clear();
		}

		private async Task ProcessQueue()
		{
			while (true)
			{
				await _signal.WaitAsync();

				if (_queue.TryDequeue(out var speech))
				{
					try
					{
						await _player.SpeakAsync(
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