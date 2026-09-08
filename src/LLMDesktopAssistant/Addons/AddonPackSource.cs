namespace LLMDesktopAssistant.Addons
{
	public enum AddonPackSource
	{
		Unknown,

		/// <summary>
		/// An implicit pack located exactly at the '%LOCALAPPDATA%/.llmassist/'.
		/// </summary>
		AppData,

		/// <summary>
		/// An implicit user-scoped pack located exactly at '~/.agents/', or similar directories.
		/// </summary>
		UserAgentsHome,

		/// <summary>
		/// An implicit workspace-scoped pack located exactly at active working directories.
		/// </summary>
		WorkingDirectory,

		/// <summary>
		/// An implicit workspace-scoped pack located exactly at '.agents/', or similar directories.
		/// </summary>
		AgentsHome,

		/// <summary>
		/// A pack provided by the chat's configuration.
		/// </summary>
		Configuration,

		/// <summary>
		/// A pack was scanned in the other pack locations (both implicit and explicit).
		/// For example: '~/.agents/packs/my-pack/'
		/// </summary>
		Scanned
	}
}
