namespace LLMDesktopAssistant.LLM.Settings
{
	[Flags]
	public enum DirectoryAccessMode
	{
		/// <summary>
		/// No access to the directory. When this flags is set to None, no operations can be performed on the directory.
		/// </summary>
		None = 0,

		/// <summary>
		/// Read/write access to the directory. Includes read and write permissions.
		/// </summary>
		ReadWrite = (Read | Write),

		/// <summary>
		/// Full access to the directory. Includes read, write, and execute permissions.
		/// </summary>
		All = (Read | Write | Execute),

		/// <summary>
		/// Read access to the directory. Allows reading files and directories within the directory.
		/// </summary>
		Read = 1 << 0,

		/// <summary>
		/// Write access to the directory. Allows writing files and directories within the directory.
		/// </summary>
		Write = 1 << 1,

		/// <summary>
		/// Execute access to the directory. Allows executing files within the directory.
		/// </summary>
		Execute = 1 << 2
	}
}
