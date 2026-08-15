namespace LLMDesktopAssistant.Tools
{
	/// <summary>
	/// Enumerates the possible sources of tools within the application.
	/// </summary>
	public enum ToolSource
	{
		/// <summary>
		/// Indicates that the tool is provided by the native application.
		/// </summary>
		Native,

		/// <summary>
		/// Indicates that the tool is provided by the MCP (Model Context Protocol) system.
		/// </summary>
		MCP,

		/// <summary>
		/// Indicates that the tool is created by LLM using meta tools.
		/// </summary>
		Meta
	}
}