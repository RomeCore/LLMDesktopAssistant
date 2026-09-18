namespace LLMDesktopAssistant.Addons.MVVM.Elements
{
	/// <summary>
	/// The view model of a block of monospaced text shown inside a card: the body of an addon,
	/// the argument schema of a tool and so on.
	/// </summary>
	[ViewModelFor(typeof(AddonCardBodyTextView))]
	public sealed class AddonCardBodyTextViewModel : NotifyPropertyChanged
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AddonCardBodyTextViewModel"/> class.
		/// </summary>
		/// <param name="text">The text to show.</param>
		public AddonCardBodyTextViewModel(string text)
		{
			Text = text;
		}

		/// <summary>
		/// Gets the text to show.
		/// </summary>
		public string Text { get; }
	}
}
