namespace LLMDesktopAssistant.Addons.MVVM
{
	/// <summary>
	/// A card change element that edits the enabled override of a single addon without the hidden
	/// override. It is used by the addon types whose hidden state is meaningless (the Lua scripts,
	/// for example, are never injected into prompts).
	/// Group cards pair the elements of their children by this interface to aggregate and edit the
	/// enabled state of the whole group.
	/// </summary>
	public interface IAddonCardEnabledChange : IAddonCardChange
	{
		/// <summary>
		/// Gets a value indicating whether the element is allowed to edit the change. Fixed addons have
		/// no editable enabled state.
		/// </summary>
		bool CanEdit { get; }

		/// <summary>
		/// Gets or sets the effective enabled state.
		/// </summary>
		bool IsEnabled { get; set; }
	}
}
