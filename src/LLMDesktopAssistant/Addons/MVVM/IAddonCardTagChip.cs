namespace LLMDesktopAssistant.Addons.MVVM
{
	/// <summary>
	/// A chip rendered as a clickable button instead of a plain bordered label.
	/// Used for tags, which apply a search filter when clicked.
	/// </summary>
	public interface IAddonCardTagChip : IAddonCardChip
	{
		/// <summary>
		/// The command executed when the chip is clicked. Chips with a command are rendered
		/// as buttons (see <see cref="IAddonCardTagChip"/>).
		/// </summary>
		ICommand? Command { get; }

		/// <summary>
		/// The parameter passed to <see cref="Command"/>.
		/// </summary>
		object? CommandParameter { get; }
	}
}
