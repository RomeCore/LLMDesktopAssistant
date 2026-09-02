namespace LLMDesktopAssistant.StructuredValues
{
	/// <summary>
	/// Represents a structured dictionary value.
	/// </summary>
	public interface INodeDictionaryValue : INodeValue
	{
		/// <summary>
		/// Gets the entries of the dictionary.
		/// </summary>
		IReadOnlyDictionary<string, INodeValue> Items { get; }
	}
}
