namespace LLMDesktopAssistant.StructuredValues
{
	/// <summary>
	/// Represents a structured boolean value.
	/// </summary>
	public interface INodeBooleanValue : INodeValue
	{
		/// <summary>
		/// Gets the boolean value.
		/// </summary>
		bool Value { get; }
	}
}
