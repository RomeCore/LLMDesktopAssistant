namespace LLMDesktopAssistant.Tools.Meta
{
	/// <summary>
	/// Interface for parsing meta tool file contents into <see cref="MetaToolInfo"/> with diagnostics.
	/// </summary>
	public interface IMetaToolParser
	{
		/// <summary>
		/// Parses the given file contents into a <see cref="MetaToolInfo"/>.
		/// </summary>
		/// <param name="filePath">The path of the tool file (used to derive the tool name).</param>
		/// <param name="contents">The raw file contents.</param>
		/// <param name="source">The source of the tool file.</param>
		/// <param name="engineDescriptor">The engine descriptor defining the frontmatter syntax.</param>
		/// <returns>The parsed tool info with diagnostics.</returns>
		MetaToolInfo Parse(string filePath, string contents, MetaToolSource source, IMetaToolEngineDescriptor engineDescriptor);

		/// <summary>
		/// Serializes a meta tool into the file format (frontmatter + execution code)
		/// defined by the given engine descriptor.
		/// </summary>
		/// <param name="tool">The tool to serialize.</param>
		/// <param name="engineDescriptor">The engine descriptor defining the frontmatter syntax.</param>
		/// <returns>The file contents.</returns>
		string Serialize(MetaToolInfo tool, IMetaToolEngineDescriptor engineDescriptor);
	}
}
