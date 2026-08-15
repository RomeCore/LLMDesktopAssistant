namespace LLMDesktopAssistant.Tools
{
	/// <summary>
	/// A custom attribute to mark a parameter as the shared context object of a tool.
	/// The shared context parameters must have <see langword="ref"/> modifier.
	/// This parameter will be shared across streaming, preview and main executors within the same tool call.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter)]
	public sealed class SharedContextAttribute : Attribute
	{
	}
}