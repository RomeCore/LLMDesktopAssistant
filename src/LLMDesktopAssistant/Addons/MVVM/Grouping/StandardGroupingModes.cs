namespace LLMDesktopAssistant.Addons.MVVM.Grouping
{
	/// <summary>
	/// The default set of the grouping modes: a flat list, grouping by category and grouping by pack.
	/// </summary>
	public static class StandardGroupingModes
	{
		/// <summary>
		/// Creates the default grouping modes for the given addon type.
		/// </summary>
		/// <typeparam name="TAddon">The type of the addon of the list.</typeparam>
		/// <returns>The modes in the order they are shown in the mode selector.</returns>
		public static ImmutableList<GroupingMode> CreateDefault<TAddon>()
			where TAddon : AddonBase<TAddon> =>
			[
				new FlatGroupingMode<TAddon>(),
				new CategoryGroupingMode<TAddon>(),
				new PackGroupingMode<TAddon>()
			];
	}
}
