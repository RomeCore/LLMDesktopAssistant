namespace LLMDesktopAssistant.Addons.MVVM
{
	/// <inheritdoc cref="IAddonCardTagChip"/>
	public class AddonCardTagChip : AddonCardChip, IAddonCardTagChip
	{
		/// <inheritdoc/>
		public ICommand? Command { get; init; }

		/// <inheritdoc/>
		public object? CommandParameter { get; init; }
	}
}
