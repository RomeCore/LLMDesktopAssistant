using LLMDesktopAssistant.Localization;
using Material.Icons;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace LLMDesktopAssistant.Tools.Implementations
{
	[ToolModule(chatScoped: false)]
	public class TimeToolModule : ToolModule
	{
		public TimeToolModule()
		{
			AddTool(TimeGet,
				new ToolInitializationInfo
				{
					Name = "time-get",
					Description = "Gets the current time in the specified timezone.",
					Category = "time"
				});

			AddTool(TimeWait,
				new ToolInitializationInfo
				{
					Name = "time-wait",
					Description = "Waits for a specified duration with real-time progress updates.",
					Category = "time",
					DefaultExpectedBehaviour = ToolBehaviour.LongRunningTask
				});
		}

		private ReactiveToolResult TimeGet(
			[Description("Timezone to get the time for (e.g., 'UTC', 'UTC+5', 'GMT+3'). Default is current user's timezone.")]
			string? timezone = null)
		{
			try
			{
				TimeZoneInfo tz;
				DateTime time;
				var timezoneId = timezone?.Trim().ToUpperInvariant();

				if (timezoneId == null)
				{
					time = DateTime.Now;
					return new ReactiveToolResult
					{
						ResultContent = time.ToString("O"),
						StatusIcon = MaterialIconKind.ClockOutline,
						StatusTitle = time.ToString("yyyy-MM-dd HH:mm:ss.fff")
					}.CompleteWithSuccess();
				}

				// Parse timezone from the format "UTC+X", "UTC-X", "GMT+X", "GMT-X" or standard timezone names
				if (timezoneId == "UTC" || timezoneId == "GMT")
				{
					tz = TimeZoneInfo.Utc;
				}
				else if (timezoneId.StartsWith("UTC+") || timezoneId.StartsWith("UTC-") ||
						 timezoneId.StartsWith("GMT+") || timezoneId.StartsWith("GMT-"))
				{
					// Extract the offset
					string offsetStr = timezoneId.Substring(3); // Remove "UTC" or "GMT"
					if (int.TryParse(offsetStr, out int hours))
					{
						try
						{
							tz = TimeZoneInfo.CreateCustomTimeZone(
								timezoneId,
								TimeSpan.FromHours(hours),
								timezoneId,
								timezoneId);
						}
						catch
						{
							return ReactiveToolResult.CreateError($"Invalid timezone offset: {timezoneId}");
						}
					}
					else
					{
						return ReactiveToolResult.CreateError($"Invalid timezone format: {timezone}. Expected format: UTC+X, UTC-X, GMT+X, GMT-X");
					}
				}
				else
				{
					// Try to find the timezone by ID
					try
					{
						tz = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
					}
					catch (TimeZoneNotFoundException)
					{
						return ReactiveToolResult.CreateError($"Timezone not found: {timezone}. Valid examples: 'UTC', 'UTC+5', 'GMT+3', 'Pacific Standard Time'");
					}
				}

				time = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);
				var display = time.ToString("yyyy-MM-dd HH:mm:ss.fff") + $" ({timezoneId})";

				return new ReactiveToolResult
				{
					ResultContent = time.ToString("O"),
					StatusIcon = MaterialIconKind.ClockOutline,
					StatusTitle = display
				}.CompleteWithSuccess();
			}
			catch (Exception ex)
			{
				return ReactiveToolResult.CreateError($"Time get error: {ex.Message}");
			}
		}

		private ReactiveToolResult TimeWait(
			[Description("Duration to wait in format 'hours:minutes:seconds' or 'minutes:seconds' (e.g., '0:05', '1:30', '1:00:10')")]
			string span = "0:00",
			CancellationToken cancellationToken = default)
		{
			try
			{
				// Parse the time span
				if (!TryParseTimeSpan(span, out TimeSpan duration))
				{
					return ReactiveToolResult.CreateError("Invalid time span format. Expected format: 'hours:minutes:seconds' or 'minutes:seconds' (e.g., '0:05', '1:30', '0:00:10')");
				}

				if (duration.TotalMilliseconds <= 0)
				{
					return new ReactiveToolResult
					{
						ResultContent = "Wait completed instantly (duration <= 0).",
						StatusIcon = Material.Icons.MaterialIconKind.TimerCheck,
						StatusTitle = string.Format(LocalizationManager.LocalizeStatic("time_wait_completed"), "0:00")
					}.CompleteWithSuccess();
				}

				var result = new ReactiveToolResult();

				Task.Run(async () =>
				{
					try
					{
						var stopwatch = System.Diagnostics.Stopwatch.StartNew();
						var totalMilliseconds = duration.TotalMilliseconds;
						var updateInterval = TimeSpan.FromMilliseconds(100); // Update every 100ms for smooth progress

						result.StatusIcon = MaterialIconKind.TimerSand;
						result.StatusTitle = string.Format(LocalizationManager.LocalizeStatic("time_wait_status"), FormatTimeSpan(duration));
						result.Progress = 0;
						result.MaxProgress = (int)totalMilliseconds;

						while (stopwatch.Elapsed < duration)
						{
							cancellationToken.ThrowIfCancellationRequested();

							var elapsed = stopwatch.Elapsed;
							var remaining = duration - elapsed;

							result.Progress = (int)Math.Min(elapsed.TotalMilliseconds, totalMilliseconds);
							result.StatusTitle = string.Format(LocalizationManager.LocalizeStatic("time_wait_status"), FormatTimeSpan(remaining));
							result.StatusIcon = MaterialIconKind.TimerSand;

							// Calculate the next update delay, but don't wait longer than remaining time
							var nextUpdateDelay = TimeSpan.FromMilliseconds(Math.Min(
								updateInterval.TotalMilliseconds,
								Math.Max(1, remaining.TotalMilliseconds)));

							await Task.Delay(nextUpdateDelay, cancellationToken);
						}

						stopwatch.Stop();

						result.ResultContent = $"Wait completed: {FormatTimeSpan(stopwatch.Elapsed)} elapsed";
						result.Progress = (int)totalMilliseconds;
						result.StatusIcon = MaterialIconKind.TimerCheck;
						result.StatusTitle = string.Format(LocalizationManager.LocalizeStatic("time_wait_completed"), FormatTimeSpan(stopwatch.Elapsed));
						result.Complete(true);
					}
					catch (OperationCanceledException)
					{
						result.StatusIcon = MaterialIconKind.TimerCancel;
						result.StatusTitle = "Wait cancelled";
						result.Complete(false);
					}
					catch (Exception ex)
					{
						result.StatusIcon = null;
						result.StatusTitle = $"Error: {ex.Message}";
						result.Complete(false);
					}
				}, CancellationToken.None);

				return result;
			}
			catch (Exception ex)
			{
				return ReactiveToolResult.CreateError($"Time wait error: {ex.Message}");
			}
		}

		private bool TryParseTimeSpan(string span, out TimeSpan result)
		{
			result = TimeSpan.Zero;

			if (string.IsNullOrWhiteSpace(span))
				return false;

			// Try parsing "MM:SS" or "H:MM:SS" format with flexible digits
			var parts = span.Split(':');

			if (parts.Length == 2)
			{
				// Format: minutes:seconds
				if (int.TryParse(parts[0], out int minutes) &&
					int.TryParse(parts[1], out int seconds))
				{
					if (minutes >= 0 && seconds >= 0)
					{
						result = new TimeSpan(0, minutes, seconds);
						return true;
					}
				}
			}
			else if (parts.Length == 3)
			{
				// Format: hours:minutes:seconds
				if (int.TryParse(parts[0], out int hours) &&
					int.TryParse(parts[1], out int minutes) &&
					int.TryParse(parts[2], out int seconds))
				{
					if (hours >= 0 && minutes >= 0 && seconds >= 0)
					{
						result = new TimeSpan(hours, minutes, seconds);
						return true;
					}
				}
			}

			return false;
		}

		private string FormatTimeSpan(TimeSpan span)
		{
			if (span.TotalHours >= 1)
				return $"{(int)span.TotalHours}:{span.Minutes:D2}:{span.Seconds:D2}";
			return $"{span.Minutes:D2}:{span.Seconds:D2}";
		}
	}
}