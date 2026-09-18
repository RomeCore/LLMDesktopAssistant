using LLMDesktopAssistant.Localization;
using Material.Icons;

namespace LLMDesktopAssistant.Addons.MVVM
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

	/// <summary>
	/// The grouping mode that groups the addons by the addon pack they were loaded from. The addons
	/// that are not part of any pack are grouped into the built-in group.
	/// </summary>
	/// <typeparam name="TAddon">The type of the addon of the list.</typeparam>
	public sealed class PackGroupingMode<TAddon> : GroupingMode<TAddon>
		where TAddon : AddonBase<TAddon>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PackGroupingMode{TAddon}"/> class.
		/// </summary>
		public PackGroupingMode()
			: base(Locale.GetKey("card.grouping.pack"), MaterialIconKind.PackageVariant)
		{
		}

		/// <inheritdoc/>
		public override AddonGroupKey? GetGroupKey(TAddon addon)
		{
			var pack = addon.SourcePack;

			if (pack is null)
			{
				return new AddonGroupKey
				{
					Id = "pack:builtin",
					Title = Locale.GetKey("card.group.builtin"),
					Icon = MaterialIconKind.PackageVariant
				};
			}

			return new AddonGroupKey
			{
				Id = $"pack:{pack.Path}",
				Title = pack.NameKey,
				Icon = MaterialIconKind.PackageVariant
			};
		}
	}

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
