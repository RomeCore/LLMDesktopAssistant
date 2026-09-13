namespace LLMDesktopAssistant.Addons.MVVM
{
	/// <summary>
	/// A plain (non-editable) header element: a slot that only displays content and never marks
	/// the addon as changed. Use <see cref="AddonCardChange"/> for header slots that edit overrides.
	/// </summary>
	public class AddonCardHeaderElement : AddonCardElementBase, IAddonCardHeaderElement
	{
		/// <inheritdoc/>
		public object? Content { get; init; }

		/// <inheritdoc/>
		public bool IsShownLeft { get; init; }

		/// <inheritdoc/>
		public bool IsChanged { get; init; }
	}
}
