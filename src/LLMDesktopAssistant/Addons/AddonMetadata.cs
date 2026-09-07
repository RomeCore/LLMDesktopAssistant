namespace LLMDesktopAssistant.Addons
{
	public class AddonMetadata : Freezable
	{
		/// <summary>
		/// The metadata associated with the addon.
		/// This dictionary can be used to store additional information about the addon, such as its version number or author.
		/// </summary>
		public ImmutableDictionary<AddonMetadataType, string> Metadata
		{
			get;
			set => SetProperty(ref field, value);
		} = [];

		/// <summary>
		/// The additional metadata associated with the addon.
		/// Used for metadata values that are not covered by <see cref="AddonMetadataType"/>.
		/// </summary>
		public ImmutableDictionary<string, string> AdditionalMetadata
		{
			get;
			set => SetProperty(ref field, value);
		} = [];

		/// <summary>
		/// The tags associated with the addon. Used for UI display and search.
		/// Examples: 'development', 'code-quality', 'refactoring'.
		/// </summary>
		public ImmutableHashSet<string> Tags
		{
			get;
			set => SetProperty(ref field, value);
		} = [];
	}
}
