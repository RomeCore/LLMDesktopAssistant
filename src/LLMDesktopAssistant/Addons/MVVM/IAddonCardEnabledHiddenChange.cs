namespace LLMDesktopAssistant.Addons.MVVM
{
	/// <summary>
	/// A card change element that edits the enabled/hidden overrides of a single addon.
	/// Group cards pair the elements of their children by this interface to aggregate and edit the
	/// states of the whole group.
	/// </summary>
	public interface IAddonCardEnabledHiddenChange : IAddonCardChange
	{
		/// <summary>
		/// Gets a value indicating whether the element is allowed to edit the change. Fixed addons have
		/// no editable enabled/hidden state.
		/// </summary>
		bool CanEdit { get; }

		/// <summary>
		/// Gets or sets the effective enabled state.
		/// </summary>
		bool IsEnabled { get; set; }

		/// <summary>
		/// Gets or sets the effective hidden state.
		/// </summary>
		bool IsHidden { get; set; }
	}
}
