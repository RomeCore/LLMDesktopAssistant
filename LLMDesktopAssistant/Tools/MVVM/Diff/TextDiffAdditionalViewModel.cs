using CommunityToolkit.Mvvm.Input;
using LiteDB;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Utils;

using LLMDesktopAssistant.Utils.Files;

namespace LLMDesktopAssistant.Tools.MVVM.Diff
{
	/// <summary>
	/// ViewModel for displaying a diff with optional confirmation controls.
	/// Supports two modes: read-only (just display) and confirmation (with checkboxes and accept/decline buttons).
	/// BSON-serializable for persistence in LiteDB when <see cref="AdditionalMessageViewModel.IsTemporary"/> is <see langword="false"/>.
	/// Stores the diff as a list of <see cref="HunkGroup"/> chunks (compatible with <see cref="UnifiedDiff.Compute"/>).
	/// </summary>
	[ViewModelFor(typeof(TextDiffAdditionalView))]
	public class TextDiffAdditionalViewModel : AdditionalMessageViewModel
	{
		private string _title = string.Empty;
		/// <summary>
		/// Gets or sets the title displayed above the diff (e.g., file path or description).
		/// </summary>
		public string Title
		{
			get => _title;
			set => SetProperty(ref _title, value);
		}

		private string _description = string.Empty;
		/// <summary>
		/// Gets or sets the description text shown below the title.
		/// </summary>
		public string Description
		{
			get => _description;
			set => SetProperty(ref _description, value);
		}

		private bool _isReadOnly = true;
		/// <summary>
		/// Gets or sets whether this diff is in read-only mode.
		/// In read-only mode, no checkboxes or action buttons are shown.
		/// In confirmation mode, users can toggle chunks and accept/decline changes.
		/// </summary>
		public bool IsReadOnly
		{
			get => _isReadOnly;
			set => SetProperty(ref _isReadOnly, value);
		}

		/// <summary>
		/// Gets or sets the list of diff chunks (hunks).
		/// This is the serialized form of the diff, compatible with BSON.
		/// Each <see cref="HunkGroup"/> contains a header and a list of <see cref="HunkLine"/>.
		/// </summary>
		public List<HunkGroup> Chunks { get; set; } = [];

		/// <summary>
		/// Gets or sets the enabled state for each chunk index.
		/// If <see langword="null"/>, all chunks are considered enabled.
		/// Used in confirmation mode to track per-chunk checkbox state.
		/// </summary>
		public List<bool>? EnabledStates { get; set; }

		private bool? _isConfirmed;
		/// <summary>
		/// Gets or sets the confirmation state.
		/// <see langword="null"/> while pending, <see langword="true"/> when accepted, <see langword="false"/> when declined.
		/// </summary>
		public bool? IsConfirmed
		{
			get => _isConfirmed;
			set
			{
				if (SetProperty(ref _isConfirmed, value))
				{
					RaisePropertyChanged(nameof(IsPending));
					RaisePropertyChanged(nameof(IsAccepted));
					RaisePropertyChanged(nameof(IsDeclined));
				}
			}
		}

		/// <summary>
		/// Gets whether the confirmation is still pending.
		/// </summary>
		[BsonIgnore]
		public bool IsPending => _isConfirmed == null;

		/// <summary>
		/// Gets whether the changes were accepted.
		/// </summary>
		[BsonIgnore]
		public bool IsAccepted => _isConfirmed == true;

		/// <summary>
		/// Gets whether the changes were declined.
		/// </summary>
		[BsonIgnore]
		public bool IsDeclined => _isConfirmed == false;

		/// <summary>
		/// Gets the total number of removed lines across all chunks.
		/// </summary>
		[BsonIgnore]
		public int TotalRemoved
		{
			get
			{
				int count = 0;
				foreach (var chunk in Chunks)
					foreach (var line in chunk.Lines)
						if (line.Kind == '-') count++;
				return count;
			}
		}

		/// <summary>
		/// Gets the total number of added lines across all chunks.
		/// </summary>
		[BsonIgnore]
		public int TotalAdded
		{
			get
			{
				int count = 0;
				foreach (var chunk in Chunks)
					foreach (var line in chunk.Lines)
						if (line.Kind == '+') count++;
				return count;
			}
		}

		/// <summary>
		/// Gets the total number of chunks that have changes.
		/// </summary>
		[BsonIgnore]
		public int ChangedChunksCount
		{
			get
			{
				int count = 0;
				foreach (var chunk in Chunks)
				{
					bool hasChanges = false;
					foreach (var line in chunk.Lines)
					{
						if (line.Kind is '+' or '-')
						{
							hasChanges = true;
							break;
						}
					}
					if (hasChanges) count++;
				}
				return count;
			}
		}

		[BsonIgnore]
		private TaskCompletionSource<bool>? _confirmationTcs;

		/// <summary>
		/// Gets the confirmation task that completes when the user accepts or declines the changes.
		/// Returns <see langword="true"/> if accepted, <see langword="false"/> if declined.
		/// </summary>
		[BsonIgnore]
		[ChangeTracker.Untracked]
		public Task<bool> ConfirmationTask
		{
			get
			{
				if (_confirmationTcs == null)
				{
					_confirmationTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
					if (_isConfirmed != null)
						_confirmationTcs.TrySetResult(_isConfirmed.Value);
				}
				return _confirmationTcs.Task;
			}
		}

		/// <summary>
		/// Command to accept all changes (confirmation mode).
		/// </summary>
		[BsonIgnore]
		public IRelayCommand? ConfirmCommand { get; private set; }

		/// <summary>
		/// Command to decline all changes (confirmation mode).
		/// </summary>
		[BsonIgnore]
		public IRelayCommand? DeclineCommand { get; private set; }

		/// <summary>
		/// Command to enable all chunks (confirmation mode).
		/// </summary>
		[BsonIgnore]
		public IRelayCommand? EnableAllCommand { get; private set; }

		/// <summary>
		/// Command to disable all chunks (confirmation mode).
		/// </summary>
		[BsonIgnore]
		public IRelayCommand? DisableAllCommand { get; private set; }

		public TextDiffAdditionalViewModel()
		{
			ConfirmCommand = new RelayCommand(Confirm);
			DeclineCommand = new RelayCommand(Decline);
			EnableAllCommand = new RelayCommand(EnableAll);
			DisableAllCommand = new RelayCommand(DisableAll);
		}

		/// <summary>
		/// Accepts all changes and marks the confirmation as complete.
		/// </summary>
		public void Confirm()
		{
			IsConfirmed = true;
			_confirmationTcs?.TrySetResult(true);
		}

		/// <summary>
		/// Declines all changes and marks the confirmation as complete.
		/// </summary>
		public void Decline()
		{
			IsConfirmed = false;
			_confirmationTcs?.TrySetResult(false);
		}

		/// <summary>
		/// Enables all chunks (checks all checkboxes).
		/// </summary>
		public void EnableAll()
		{
			if (EnabledStates == null) return;
			for (int i = 0; i < EnabledStates.Count; i++)
				EnabledStates[i] = true;
		}

		/// <summary>
		/// Disables all chunks (unchecks all checkboxes).
		/// </summary>
		public void DisableAll()
		{
			if (EnabledStates == null) return;
			for (int i = 0; i < EnabledStates.Count; i++)
				EnabledStates[i] = false;
		}

		/// <summary>
		/// Ensures that <see cref="EnabledStates"/> list is initialized with the same count as <see cref="Chunks"/>.
		/// Call this after setting <see cref="Chunks"/> if you plan to use confirmation mode.
		/// </summary>
		public void EnsureEnabledStates()
		{
			EnabledStates = [];
			for (int i = 0; i < Chunks.Count; i++)
				EnabledStates.Add(true);
		}

		/// <summary>
		/// Gets a value indicating whether a specific chunk is enabled.
		/// </summary>
		public bool IsChunkEnabled(int index)
		{
			return EnabledStates == null || (index >= 0 && index < EnabledStates.Count && EnabledStates[index]);
		}

		/// <summary>
		/// Loads the diff from a <see cref="HunkGroups"/> result (produced by <see cref="UnifiedDiff.Compute"/>).
		/// </summary>
		/// <param name="groups">The diff groups to load.</param>
		public void LoadFromHunkGroups(HunkGroups groups)
		{
			Chunks = groups.Groups ?? [];
			EnsureEnabledStates();
		}

		/// <summary>
		/// Builds a <see cref="HunkGroups"/> from the (enabled) chunks for further processing.
		/// </summary>
		/// <returns>A <see cref="HunkGroups"/> containing only the enabled chunks.</returns>
		public HunkGroups BuildHunkGroups()
		{
			var result = new List<HunkGroup>();
			for (int i = 0; i < Chunks.Count; i++)
			{
				if (IsChunkEnabled(i))
					result.Add(Chunks[i]);
			}
			return new HunkGroups { Groups = result };
		}

		/// <summary>
		/// Returns the full diff text representation.
		/// </summary>
		public string BuildDiffText()
		{
			var sb = new System.Text.StringBuilder();
			foreach (var chunk in Chunks)
			{
				sb.AppendLine(chunk.ToString());
			}
			return sb.ToString();
		}

		/// <summary>
		/// Applies the (enabled) diff chunks to the original text and returns the modified result.
		/// </summary>
		/// <param name="originalText">The original text content before changes.</param>
		/// <returns>The modified text after applying all enabled diff chunks.</returns>
		public string ApplyToText(string originalText)
		{
			return BuildHunkGroups().ApplyToText(originalText);
		}
	}
}
