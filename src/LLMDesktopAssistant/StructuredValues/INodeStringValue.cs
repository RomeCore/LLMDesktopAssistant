namespace LLMDesktopAssistant.StructuredValues
{
	/// <summary>
	/// Represents a structured string value.
	/// </summary>
	public interface INodeStringValue : INodeValue
	{
		/// <summary>
		/// Gets the string value.
		/// </summary>
		string? Value { get; }
	}
}
