using LLMDesktopAssistant.Localization;
using Material.Icons;

namespace LLMDesktopAssistant.Addons.MVVM.Grouping
{
	/// <summary>
	/// The grouping mode that does not group anything: every card is shown on the top level of the list.
	/// </summary>
	/// <typeparam name="TAddon">The type of the addon of the list.</typeparam>
	public sealed class FlatGroupingMode<TAddon> : GroupingMode<TAddon>
		where TAddon : AddonBase<TAddon>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="FlatGroupingMode{TAddon}"/> class.
		/// </summary>
		public FlatGroupingMode()
			: base(Locale.GetKey("card.grouping.flat"), MaterialIconKind.FormatListBulleted)
		{
		}

		/// <inheritdoc/>
		public override AddonGroupKey? GetGroupKey(TAddon addon) => null;
	}
}
