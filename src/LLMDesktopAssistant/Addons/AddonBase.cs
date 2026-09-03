using System.Text.Json.Serialization;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.StructuredValues.Const;

namespace LLMDesktopAssistant.Addons
{
	/// <summary>
	/// Represents a base class of all agentic addons (tools, skills, prompt parts, sub-agents, commands, etc.).
	/// </summary>
	public abstract class AddonBase<Self> : NotifyPropertyChanged
		where Self : AddonBase<Self>
	{
		/// <summary>
		/// The name used to identify the addon for deduplication and agent selection purposes.
		/// Usually has constraints like only alpha-numeric characters, underscores and hyphens.
		/// </summary>
		public required string Name { get; init; }

		/// <summary>
		/// The short agent-readable description of the addon. Used by agent for understanding when to use this addon.
		/// </summary>
		[JsonIgnore]
		public Func<Self, string> DescriptionGetter { get; init; } = s => string.Empty;

		/// <summary>
		/// The short agent-readable description of the addon. Used by agent for understanding when to use this addon.
		/// </summary>
		public string Description
		{
			get => DescriptionGetter((Self)this);
			init => DescriptionGetter = s => value;
		}

		/// <summary>
		/// The addon file content getter, excluding the frontmatter.
		/// </summary>
		[JsonIgnore]
		public Func<Self, string> BodyGetter { get; init; } = s => string.Empty;

		/// <summary>
		/// The addon file content getter, excluding the frontmatter.
		/// </summary>
		public string Body
		{
			get => BodyGetter((Self)this);
			init => BodyGetter = s => value;
		}

		/// <summary>
		/// The locale key for the name. If not set, it will be automatically generated from the name.
		/// Used for user-friendly display.
		/// </summary>
		public LocaleKeyBase NameKey
		{
			get => field ??= Locale.GetConstKey(Name);
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// The locale key for the description. If not set, it will be automatically generated from the description.
		/// Used for user-friendly display of the addon's description.
		/// </summary>
		public LocaleKeyBase DescriptionKey
		{
			get => field ?? Locale.GetConstKey(DescriptionGetter((Self)this));
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// The category of the addon. Used for organizing and filtering addons in the UI.
		/// </summary>
		public LocaleKeyBase? CategoryKey
		{
			get;
			set => SetProperty(ref field, value);
		}

		// ===================================
		// === Directories and paths       ===
		// ===================================

		/// <summary>
		/// Full path to main addon file, if applicable. This is 'SKILL.md' file for skills, 'my-tool.lua' for metatools.
		/// </summary>
		public string? Path { get; init; } = null;

		/// <summary>
		/// The home directory of the addon. If not set, it will be automatically generated from the path.
		/// </summary>
		public string? HomeDirectory
		{
			get => field ??= System.IO.Path.GetDirectoryName(Path);
			init;
		}

		// ===================================
		// === Metadata                    ===
		// ===================================

		/// <summary>
		/// The metadata associated with the addon.
		/// This dictionary can be used to store additional information about the addon, such as its version number or author.
		/// </summary>
		public ImmutableDictionary<AddonMetadataType, string> Metadata { get; init; } = [];

		/// <summary>
		/// The additional metadata associated with the addon.
		/// Used for metadata values that are not covered by <see cref="AddonMetadataType"/>.
		/// </summary>
		public ImmutableDictionary<string, string> AdditionalMetadata { get; init; } = [];

		/// <summary>
		/// The tags associated with the addon. Used for UI display and search.
		/// Examples: 'development', 'code-quality', 'refactoring'.
		/// </summary>
		public ImmutableList<string> Tags { get; init; } = [];

		/// <summary>
		/// The additional properties associated with the addon.
		/// Used for root properties that are not covered by other properties of this class.
		/// </summary>
		public ImmutableDictionary<string, ConstNodeValue> AdditionalProperties { get; init; } = [];

		// ===================================
		// === Diagnostics                 ===
		// ===================================

		private AddonDiagnostic? _diagnostic = null;
		/// <summary>
		/// The diagnostic information associated with the addon.
		/// </summary>
		[JsonIgnore]
		public AddonDiagnostic? Diagnostic
		{
			get => _diagnostic;
			set => SetProperty(ref _diagnostic, value);
		}

	}
}
