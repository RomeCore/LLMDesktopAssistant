namespace LLMDesktopAssistant.Tools.Meta
{
	/// <summary>
	/// The source of a meta tool file.
	/// </summary>
	public enum MetaToolSource
	{
		/// <summary>
		/// The tool is stored in the application-wide directory (<see cref="Utils.Directories.Metatools"/>).
		/// </summary>
		UserProfile,

		/// <summary>
		/// The tool is stored inside a working directory (<c>.llmassist/metatools</c>).
		/// </summary>
		WorkingDirectory,

		/// <summary>
		/// The tool is stored in a user-specified additional directory or file.
		/// </summary>
		Custom
	}
}
