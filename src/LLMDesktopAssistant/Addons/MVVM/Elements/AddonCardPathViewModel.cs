namespace LLMDesktopAssistant.Addons.MVVM.Elements
{
	/// <summary>
	/// The view model of the path label of a file-backed addon, shown on the left side of the action row.
	/// </summary>
	[ViewModelFor(typeof(AddonCardPathView))]
	public sealed class AddonCardPathViewModel : NotifyPropertyChanged
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AddonCardPathViewModel"/> class.
		/// </summary>
		/// <param name="path">The path of the addon file.</param>
		public AddonCardPathViewModel(string path)
		{
			Path = path;
		}

		/// <summary>
		/// Gets the path of the addon file.
		/// </summary>
		public string Path { get; }
	}
}
