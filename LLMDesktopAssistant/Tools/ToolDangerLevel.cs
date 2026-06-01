namespace LLMDesktopAssistant.Tools
{
	/// <summary>
	/// Represents the danger level of a tool during pre-execution.
	/// </summary>
	public enum ToolDangerLevel
	{
		/// <summary>
		/// The default danger level. This is used when no other level applies.
		/// </summary>
		Default,

		/// <summary>
		/// Indicates that the tool is safe to use. This means that it does not pose a risk to the user or their system.
		/// </summary>
		Safe,

		/// <summary>
		/// Indicates that the tool may cause some inconvenience or disruption, but is generally safe to use.
		/// </summary>
		Warning,

		/// <summary>
		/// Indicates that the tool is potentially harmful or dangerous and should be used with caution.
		/// </summary>
		Dangerous
	}
}