namespace LLMDesktopAssistant.Addons.MVVM
{
	/// <summary>
	/// Builds the addon card of a single addon type. One factory per addon type, placed next to the
	/// type itself (the skills factory lives with the skills, the sub-agent factory with the sub-agents).
	/// </summary>
	/// <remarks>
	/// The factory is a DI service (see <c>AddonCardFactoriesConfigurator</c>) and receives the services
	/// it needs through its constructor. Everything that belongs to a single card (the addon, its
	/// change set, the page callbacks) is passed through the <see cref="AddonCardContext{TAddon, TChange}"/>.
	/// </remarks>
	/// <typeparam name="TAddon">The type of the addon the factory builds cards for.</typeparam>
	/// <typeparam name="TChange">The type of the change (override) object of that addon.</typeparam>
	public interface IAddonCardFactory<TAddon, TChange>
		where TAddon : AddonChangedBase<TAddon, TChange>
		where TChange : AddonChangeBase, new()
	{
		/// <summary>
		/// Creates the card for the addon of the given context.
		/// </summary>
		/// <param name="context">The addon, its change set and the page that owns the card.</param>
		/// <returns>The card view model.</returns>
		AddonCardViewModel Create(AddonCardContext<TAddon, TChange> context);

		/// <summary>
		/// Creates the card of the group of addons of the given context. The group card holds the cards
		/// of its children and aggregates only their header changes.
		/// </summary>
		/// <param name="context">The group, its addons and the cards of the children.</param>
		/// <returns>The group card view model.</returns>
		AddonCardViewModel CreateGroup(AddonGroupCardContext<TAddon, TChange> context);
	}
}
