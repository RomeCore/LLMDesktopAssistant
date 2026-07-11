using LiteDB;
using LLMDesktopAssistant.Utils.Files;

namespace LLMDesktopAssistant.Tools.MVVM.Diff
{
	/// <summary>
	/// ViewModel wrapping a single diff hunk group with an observable enabled state.
	/// Allows the user to toggle individual chunks on/off in the diff UI.
	/// </summary>
	public class HunkGroupViewModel : NotifyPropertyChanged
	{
		/// <summary>
		/// Gets the underlying hunk group data.
		/// </summary>
		public HunkGroup Group { get; }

		/// <summary>
		/// Gets the number of lines removed in this hunk group.
		/// </summary>
		[BsonIgnore]
		public int Removed { get; }

		/// <summary>
		/// Gets the number of lines added in this hunk group.
		/// </summary>
		[BsonIgnore]
		public int Added { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="HunkGroupViewModel"/> class.
		/// </summary>
		/// <param name="group">The underlying hunk group data.</param>
		/// <param name="isEnabled">Initial enabled state.</param>
		[BsonCtor]
		public HunkGroupViewModel(HunkGroup group, bool isEnabled = true)
		{
			Group = group;
			(Removed, Added) = group.GetChangeCounts();
			_isEnabled = isEnabled;
		}

		private bool _isEnabled;
		/// <summary>
		/// Gets or sets whether this chunk is enabled (included when applying changes).
		/// </summary>
		public bool IsEnabled
		{
			get => _isEnabled;
			set => SetProperty(ref _isEnabled, value);
		}
	}
}
