namespace LLMDesktopAssistant.StructuredValues
{
	/// <summary>
	/// Represents a structured number value.
	/// </summary>
	public interface INodeNumberValue : INodeValue
	{
		/// <summary>
		/// Gets the number value.
		/// </summary>
		double Value { get; }
	}
}
