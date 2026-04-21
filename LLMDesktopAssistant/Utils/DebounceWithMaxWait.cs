using System.Diagnostics;

namespace LLMDesktopAssistant.Utils
{
	/// <summary>
	/// Provides an asynchronous debouncing mechanism with a maximum wait guarantee (debounce + throttle).
	/// Ensures execution occurs after a pause in calls OR at least every maxWait interval.
	/// Perfect for scenarios like: "save to DB at least every 500ms during rapid updates".
	/// </summary>
	public class DebounceWithMaxWait : IDisposable
	{
		private CancellationTokenSource _cts = new CancellationTokenSource();
		private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
		private readonly bool _defaultResult;
		private bool _isDisposed;

		private DateTime _lastExecutionTime;
		private TimeSpan _maxWait;
		private TimeSpan _debounceDelay;
		private bool _isMaxWaitEnabled;

		/// <summary>
		/// Gets the cancellation token that signals cancellation after the debounce period or when a new call occurs.
		/// </summary>
		public CancellationToken CancellationToken => _cts.Token;

		/// <summary>
		/// Initializes a new instance with debounce delay and optional max wait.
		/// </summary>
		/// <param name="debounceDelay">Time to wait after last call before execution.</param>
		/// <param name="maxWait">Maximum time between executions even if calls continue (default: null = disabled).</param>
		/// <param name="defaultResult">The value returned when the period completes successfully.</param>
		public DebounceWithMaxWait(TimeSpan debounceDelay, TimeSpan? maxWait = null, bool defaultResult = false)
		{
			_debounceDelay = debounceDelay;
			_maxWait = maxWait ?? TimeSpan.Zero;
			_isMaxWaitEnabled = maxWait.HasValue && maxWait.Value > TimeSpan.Zero;
			_defaultResult = defaultResult;
			_lastExecutionTime = DateTime.UtcNow;
		}

		/// <summary>
		/// Debounces an action with guaranteed maximum wait between executions.
		/// Returns true if debounce period completed, false if cancelled by new call or maxWait enforcement.
		/// </summary>
		public async Task<bool> DebounceAsync(CancellationToken externalCancellationToken = default)
		{
			if (_isDisposed)
				throw new ObjectDisposedException(nameof(DebounceWithMaxWait));

			CancellationTokenSource? oldCts = null;
			bool shouldExecuteNow = false;

			try
			{
				await _semaphore.WaitAsync(externalCancellationToken).ConfigureAwait(false);

				var now = DateTime.UtcNow;
				var timeSinceLastExecution = now - _lastExecutionTime;

				// Check if maxWait interval has expired
				if (_isMaxWaitEnabled && timeSinceLastExecution >= _maxWait)
				{
					shouldExecuteNow = true;
					_lastExecutionTime = now;
				}

				oldCts = _cts;
				_cts = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken);
				oldCts.Cancel();
			}
			catch (OperationCanceledException)
			{
				return !_defaultResult;
			}
			finally
			{
				_semaphore.Release();
				oldCts?.Dispose();
			}

			if (shouldExecuteNow)
			{
				return _defaultResult;
			}

			try
			{
				await Task.Delay(_debounceDelay, _cts.Token).ConfigureAwait(false);
				_lastExecutionTime = DateTime.UtcNow;
				return _defaultResult;
			}
			catch (TaskCanceledException)
			{
				return !_defaultResult;
			}
		}

		public void Dispose()
		{
			if (_isDisposed)
				return;
			_isDisposed = true;

			_cts.Cancel();
			_cts.Dispose();
			_semaphore.Dispose();
			GC.SuppressFinalize(this);
		}
	}
}