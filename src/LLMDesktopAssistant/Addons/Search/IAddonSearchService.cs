namespace LLMDesktopAssistant.Addons.Search
{
	/// <summary>
	/// Searches addons by a free-form query over their name, description and tags.
	/// </summary>
	/// <remarks>
	/// The interface is intentionally not constrained: it can be referenced for any addon type, while the
	/// default <see cref="AddonSearchService{T}"/> implementation requires the addon CLR type to be an
	/// <see cref="AddonBase{T}"/>. The default implementation is registered as a chat- and app-scoped
	/// closed generic service for every addon type that opts in with
	/// <see cref="IAddonTypeDescriptor.UseDefaultSearchService"/>.
	/// </remarks>
	/// <typeparam name="T">The type of the addon to search.</typeparam>
	public interface IAddonSearchService<T>
	{
		/// <summary>
		/// Searches the given addons for the ones relevant to <paramref name="query"/>.
		/// </summary>
		/// <param name="query">The free-form search query. An empty query returns no results.</param>
		/// <param name="candidates">The addons to search in.</param>
		/// <param name="maxResults">The maximum number of results to return, or a non-positive value to
		/// return all matches.</param>
		/// <returns>The matching addons ordered by descending relevance.</returns>
		IReadOnlyList<AddonSearchResult<T>> Search(string query, IEnumerable<T> candidates, int maxResults = 10);
	}
}
