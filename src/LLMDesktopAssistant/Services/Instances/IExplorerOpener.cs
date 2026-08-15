namespace LLMDesktopAssistant.Services.Instances
{
	/// <summary>
	/// Interface for opening files and directories in the system file explorer.
	/// </summary>
	public interface IExplorerOpener
	{
		/// <summary>
		/// Opens a path in the system file explorer, choosing the most appropriate action:
		/// a file is revealed and selected, a directory is opened, and a non-existent path
		/// falls back to opening its parent directory (if any).
		/// </summary>
		/// <param name="path">The file or directory path to open.</param>
		/// <returns><see langword="true"/> if the path was opened successfully; otherwise, <see langword="false"/>.</returns>
		bool OpenPath(string path);

		/// <summary>
		/// Opens a directory in the system file explorer.
		/// </summary>
		/// <param name="directoryPath">The directory path to open.</param>
		/// <returns><see langword="true"/> if the directory was opened successfully; otherwise, <see langword="false"/>.</returns>
		bool OpenDirectory(string directoryPath);

		/// <summary>
		/// Opens the folder containing the specified file in the system file explorer and selects the file.
		/// </summary>
		/// <param name="filePath">The file path to reveal.</param>
		/// <returns><see langword="true"/> if the file was revealed successfully; otherwise, <see langword="false"/>.</returns>
		bool ShowFileInExplorer(string filePath);
	}
}
