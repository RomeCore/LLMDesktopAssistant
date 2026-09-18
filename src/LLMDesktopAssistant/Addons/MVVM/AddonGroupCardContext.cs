namespace LLMDesktopAssistant.Addons.MVVM
{
	/// <summary>
	/// Everything a card factory needs to build the card of one group of the list: the group itself and
	/// the cards of its children (already built by the list).
	/// </summary>
	/// <typeparam name="TAddon">The type of the addon the group holds.</typeparam>
	/// <typeparam name="TChange">The type of the change (override) object of that addon.</typeparam>
	public sealed class AddonGroupCardContext<TAddon, TChange>
		where TAddon : AddonChangedBase<TAddon, TChange>
		where TChange : AddonChangeBase, new()
	{
		/// <summary>
		/// Gets the identity and the display data of the group.
		/// </summary>
		public required AddonGroupKey Key { get; init; }

		/// <summary>
		/// Gets the addons of the group.
		/// </summary>
		public required ImmutableList<TAddon> Addons { get; init; }

		/// <summary>
		/// Gets the cards of the addons of the group, in the order of the list.
		/// </summary>
		public required ImmutableList<AddonCardViewModel> Children { get; init; }
	}
}
