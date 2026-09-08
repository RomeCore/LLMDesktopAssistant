using LLMDesktopAssistant.Localization;

namespace LLMDesktopAssistant.Addons
{
	public class AddonPackInfo : AddonMetadata
	{
		/// <summary>
		/// The name of the addon pack. Usually this is folder name or name got from pack.json manifest.
		/// </summary>
		public required string Name { get; init; }

		/// <summary>
		/// The description of the addon pack.
		/// </summary>
		public string? Description { get; init; }

		/// <summary>
		/// The path to the addon pack. This is the full path to the folder containing the addon pack.
		/// Used for identifying the addon pack.
		/// </summary>
		public required string Path { get; init; }

		/// <summary>
		/// Whether the manifest of the addon pack is valid.
		/// If null - manifest is not exists. If false - manifest is invalid. If true - manifest is valid.
		/// </summary>
		public bool? IsManifestValid { get; init; }

		/// <summary>
		/// The source type of the addon pack.
		/// </summary>
		public required AddonPackSource Source { get; init; }

		/// <summary>
		/// Whether this pack can be enabled or disabled by the user via the settings UI.
		/// Non-configurable (implicit) packs are always enabled and shown for informational purposes only.
		/// </summary>
		public required bool IsConfigurable { get; init; }

		/// <summary>
		/// The locale key for the name. If not set, it will be automatically generated from the name.
		/// Used for user-friendly display.
		/// </summary>
		[NotFrozen]
		public LocaleKeyBase NameKey
		{
			get => field ??= Locale.GetConstKey(Name);
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// The locale key for the description. If not set, it will be automatically generated from the description.
		/// Used for user-friendly display of the addon pack's description.
		/// </summary>
		[NotFrozen]
		public LocaleKeyBase? DescriptionKey
		{
			get => field ??= Description is not null ? Locale.GetConstKey(Description) : null;
			set => SetProperty(ref field, value);
		}

	}
}
