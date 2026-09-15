namespace LLMDesktopAssistant.Addons.Loading
{
	public interface IAddonFileLocator
	{
		/// <summary>
		/// Gets the folder names to search for addons inside pack directories.
		/// Examples: 'skills', 'agents', 'tools'.
		/// </summary>
		string[] Folders { get; }

		/// <summary>
		/// Gets the file extensions to search for addons (with or without a dot).
		/// Examples: '.md', '.txt', '.llt', '.hbs', '.lua', '.py'.
		/// </summary>
		/// <remarks>
		/// IMPORTANT: These extensions should be placed in a specific order based on their priority.
		/// First extensions have higher priority than later ones.
		/// </remarks>
		string[] Extensions { get; }

		/// <summary>
		/// Gets whether to allow short format names for addons.
		/// </summary>
		bool AllowShortFormat { get; }

		/// <summary>
		/// Gets the full format name of the addon file without extension.
		/// If provided, the locator will try to search for '.agents/folder_name/addon_name/FORMAT_NAME.ext'.
		/// If not provided, the locator won't search for a full format.
		/// Examples: 'SKILL' (for SKILL.md or SKILL.mdx), 'AGENT', 'BLOCK'
		/// </summary>
		string? FullFormatName { get; }
	}

	/// <summary>
	/// Locates all addon files for the effective configuration of a specific addon type.
	/// </summary>
	/// <typeparam name="T">The addon type that this locator is responsible for.</typeparam>
	public interface IAddonFileLocator<T> : IAddonFileLocator
	{
		/// <summary>
		/// Locates all addon files for effective configuration.
		/// </summary>
		/// <remarks>
		/// Note that locator can return non-existing files and directories (if user explicitly specified them),
		/// which should be handled by the caller (produce 'failed' status for addons with diagnostic info).
		/// </remarks>
		IEnumerable<AddonPathInfo> LocateFiles(AddonFileLocatorConfiguration config);
	}
}
