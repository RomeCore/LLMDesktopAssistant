using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Utils;
using Material.Icons;
using RCLargeLanguageModels.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Tools
{
	/// <summary>
	/// Represents the streaming, advanced reactive result of a tool execution.
	/// Supports brief status icons and progress updates, title updates, and streaming content.
	/// Also supports advanced human-in-the-loop features like forms, prompts and fast answers.
	/// </summary>
	public class ReactiveToolResult : NotifyPropertyChanged
	{
		private readonly TaskCompletionSource<bool> _completionSource = new();
		private readonly Lock _lock = new();

		/// <summary>
		/// The task that will be completed when the tool execution is done.
		/// Task value is true if the tool executed successfully, and false if it failed.
		/// </summary>
		public Task<bool> Completion => _completionSource.Task;

		private double? _progress = 0;
		/// <summary>
		/// The progress of the tool execution. If null, progress is indeterminate.
		/// </summary>
		public double? Progress
		{
			get => _progress;
			set
			{
				lock (_lock)
					SetProperty(ref _progress, value);
			}
		}

		private double _minProgress = 0.0;
		/// <summary>
		/// The minimum progress of the tool execution. Defaults to 0.0.
		/// </summary>
		public double MinProgress
		{
			get => _minProgress;
			set
			{
				lock (_lock)
					SetProperty(ref _minProgress, value);
			}
		}

		private double _maxProgress = 1.0;
		/// <summary>
		/// The maximum progress of the tool execution. Defaults to 1.0.
		/// </summary>
		public double MaxProgress
		{
			get => _maxProgress;
			set
			{
				lock (_lock)
					SetProperty(ref _maxProgress, value);
			}
		}

		private MaterialIconKind? _statusIcon;
		/// <summary>
		/// The status icon to be displayed. This will be shown next to the main title (that contains tool name).
		/// </summary>
		public MaterialIconKind? StatusIcon
		{
			get => _statusIcon;
			set
			{
				lock (_lock)
					SetProperty(ref _statusIcon, value);
			}
		}

		private string? _statusTitle;
		/// <summary>
		/// The title of the status that will be shown next to the main title (that contains tool name).
		/// </summary>
		public string? StatusTitle
		{
			get => _statusTitle;
			set
			{
				lock (_lock)
					SetProperty(ref _statusTitle, value);
			}
		}

		/// <summary>
		/// The observable collection of lines that contains the streaming output of the tool.
		/// Each line can contain multiple lines of text, not only a single line.
		/// Perfect for long-running tools like Python package installation.
		/// </summary>
		public RangeObservableCollection<string> ResultContentLines { get; } = [];

		/// <summary>
		/// The combined string of all lines in <see cref="ResultContentLines"/>, joined by <see cref="Environment.NewLine"/>.
		/// </summary>
		public string ResultContent
		{
			get => string.Join(Environment.NewLine, ResultContentLines);
			set
			{
				ResultContentLines.Clear();
				ResultContentLines.Add(value);
			}
		}

		/// <summary>
		/// Completes the task with a result indicating whether the tool executed successfully.
		/// </summary>
		/// <param name="success">Whether the tool executed successfully.</param>
		/// <exception cref="InvalidOperationException">Thrown if the task has already been completed.</exception>
		public ReactiveToolResult Complete(bool success)
		{
			lock (_lock)
				if (!_completionSource.TrySetResult(success))
					throw new InvalidOperationException("Tool execution already completed.");
			return this;
		}

		/// <summary>
		/// Creates a new instance of the <see cref="ReactiveToolResult"/> class with a success status and the specified output.
		/// </summary>
		/// <param name="result">The output of the tool.</param>
		/// <returns>A newly created <see cref="ReactiveToolResult"/>.</returns>
		public static ReactiveToolResult Create(bool success, string result)
		{
			var resultObj = new ReactiveToolResult();
			resultObj.ResultContent = result;
			resultObj.Complete(success: success);
			return resultObj;
		}

		/// <summary>
		/// Creates a new instance of the <see cref="ReactiveToolResult"/> class with a success status and the specified output.
		/// </summary>
		/// <param name="result">The output of the tool.</param>
		/// <returns>A newly created <see cref="ReactiveToolResult"/>.</returns>
		public static ReactiveToolResult CreateSuccess(string result)
		{
			return Create(true, result);
		}

		/// <summary>
		/// Creates a new instance of the <see cref="ReactiveToolResult"/> class with an error status and the specified output.
		/// </summary>
		/// <param name="result">The output of the tool.</param>
		/// <returns>A newly created <see cref="ReactiveToolResult"/>.</returns>
		public static ReactiveToolResult CreateError(string result)
		{
			return Create(false, result);
		}

		/// <summary>
		/// Creates a new instance of the <see cref="ReactiveToolResult"/> class from a <see cref="ToolResult"/>.
		/// </summary>
		/// <param name="result">The <see cref="ToolResult"/> to create the instance from.</param>
		/// <returns>A newly created <see cref="ReactiveToolResult"/>.</returns>
		public static ReactiveToolResult CreateFromResult(ToolResult result)
		{
			var resultObj = new ReactiveToolResult();
			resultObj.ResultContent = result.Content;

			var success = result.Status switch
			{
				ToolResultStatus.Success => true,
				ToolResultStatus.Error => false,
				ToolResultStatus.Cancelled => false,
				ToolResultStatus.NoResult => false,
				_ => false
			};

			resultObj.Complete(success: success);
			return resultObj;
		}
	}
}