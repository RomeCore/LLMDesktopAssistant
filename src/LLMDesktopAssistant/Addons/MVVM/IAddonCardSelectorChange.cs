namespace LLMDesktopAssistant.Addons.MVVM
{
	/// <summary>
	/// A card change element that edits a single (usually enum or string) override of an addon with
	/// a selector. Group cards pair the elements of their children by this interface to aggregate and
	/// edit the values of the whole group.
	/// </summary>
	/// <typeparam name="TValue">The type of the edited field (the nullable field type, e.g. <c>ToolApprovalLevel?</c> or <c>string</c>).</typeparam>
	public interface IAddonCardSelectorChange<TValue> : IAddonCardChange
	{
		/// <summary>
		/// Gets a value indicating whether the element is allowed to edit the change.
		/// </summary>
		bool CanEdit { get; }

		/// <summary>
		/// Gets the effective value of the edited field: the override value when it exists, otherwise
		/// the reference value read from the addon definition or the set default.
		/// </summary>
		TValue EffectiveValue { get; }

		/// <summary>
		/// Applies the value as the override of the edited field, unless the effective value already
		/// matches it.
		/// </summary>
		/// <param name="value">The value to apply.</param>
		void SetEffectiveValue(TValue value);
	}
}
