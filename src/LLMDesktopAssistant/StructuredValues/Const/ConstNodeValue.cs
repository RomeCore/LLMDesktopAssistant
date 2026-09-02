namespace LLMDesktopAssistant.StructuredValues.Const
{
	/// <summary>
	/// Base class for immutable structured node values.
	/// </summary>
	public abstract class ConstNodeValue : INodeValue
	{
		/// <inheritdoc />
		public abstract object? TakeValueSnapshot();
	}
}
