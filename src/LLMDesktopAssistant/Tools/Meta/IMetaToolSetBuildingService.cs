namespace LLMDesktopAssistant.Tools.Meta
{
	/// <summary>
	/// Provides access to the meta tools available in the current session.
	/// </summary>
	public interface IMetaToolSetBuildingService
	{
		/// <summary>
		/// Returns all meta tools available in the current session, deduplicated by name.
		/// </summary>
		/// <returns>A collection of <see cref="MetaToolInfo"/> objects.</returns>
		IEnumerable<MetaToolInfo> GetAvailableMetaTools();
	}
}
