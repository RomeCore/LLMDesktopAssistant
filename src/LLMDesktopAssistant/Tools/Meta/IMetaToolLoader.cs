namespace LLMDesktopAssistant.Tools.Meta
{
	/// <summary>
	/// Interface for loading meta tool files into <see cref="MetaToolInfo"/> with caching.
	/// </summary>
	public interface IMetaToolLoader
	{
		/// <summary>
		/// Loads the given meta tool files, returning their parsed infos.
		/// Files with unknown extensions are skipped; files that cannot be read or parsed
		/// are returned with a fatal diagnostic.
		/// </summary>
		/// <param name="files">The files to load.</param>
		/// <returns>The loaded tool infos.</returns>
		IEnumerable<MetaToolInfo> Load(IEnumerable<MetaToolFileInfo> files);
	}
}
