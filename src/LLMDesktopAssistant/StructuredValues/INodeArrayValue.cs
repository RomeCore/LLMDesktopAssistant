namespace LLMDesktopAssistant.StructuredValues
{
	/// <summary>
	/// Represents a structured array value.
	/// </summary>
	public interface INodeArrayValue : INodeValue
	{
		/// <summary>
		/// Gets the items of the array.
		/// </summary>
		IReadOnlyList<INodeValue> Items { get; }
	}
}
