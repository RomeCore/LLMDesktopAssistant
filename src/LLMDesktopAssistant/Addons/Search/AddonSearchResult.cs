namespace LLMDesktopAssistant.Addons.Search
{
	/// <summary>
	/// Represents a single match found by an <see cref="IAddonSearchService{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of the matched addon.</typeparam>
	public sealed class AddonSearchResult<T>
	{
		/// <summary>
		/// Gets the matched addon. It is the same instance that was passed in the candidate list.
		/// </summary>
		public required T Addon { get; init; }

		/// <summary>
		/// Gets the relevance score of the match. The scores are only comparable within a single search.
		/// </summary>
		public required double Score { get; init; }
	}
}
