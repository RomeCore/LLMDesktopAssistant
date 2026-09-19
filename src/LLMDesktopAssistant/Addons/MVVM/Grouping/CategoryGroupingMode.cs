using LLMDesktopAssistant.Localization;
using Material.Icons;

namespace LLMDesktopAssistant.Addons.MVVM.Grouping
{
	/// <summary>
	/// The grouping mode that groups the addons by the category declared by every addon.
	/// </summary>
	/// <typeparam name="TAddon">The type of the addon of the list.</typeparam>
	public sealed class CategoryGroupingMode<TAddon> : GroupingMode<TAddon>
		where TAddon : AddonBase<TAddon>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CategoryGroupingMode{TAddon}"/> class.
		/// </summary>
		public CategoryGroupingMode()
			: base(Locale.GetKey("card.grouping.category"), MaterialIconKind.FolderOutline)
		{
		}

		/// <inheritdoc/>
		public override AddonGroupKey? GetGroupKey(TAddon addon) => new()
		{
			Id = $"category:{addon.CategoryKey?.RawValue ?? "unknown"}",
			Title = addon.CategoryKey ?? Locale.GetKey("card.group.uncategorized"),
			Icon = MaterialIconKind.FolderOutline
		};
	}
}
