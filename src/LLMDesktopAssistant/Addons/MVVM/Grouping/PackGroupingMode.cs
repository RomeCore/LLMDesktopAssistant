using LLMDesktopAssistant.Localization;
using Material.Icons;

namespace LLMDesktopAssistant.Addons.MVVM.Grouping
{
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
}
