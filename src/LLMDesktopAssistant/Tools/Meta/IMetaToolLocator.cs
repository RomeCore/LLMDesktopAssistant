namespace LLMDesktopAssistant.Tools.Meta
{
	/// <summary>
	/// Interface for locating meta tool files in the application and working directories.
	/// </summary>
	public interface IMetaToolLocator
	{
		/// <summary>
		/// Locates all meta tool files available in the current session.
		/// </summary>
		/// <returns>A collection of meta tool file infos.</returns>
		IEnumerable<MetaToolFileInfo> LocateMetaToolFiles();
	}
}
