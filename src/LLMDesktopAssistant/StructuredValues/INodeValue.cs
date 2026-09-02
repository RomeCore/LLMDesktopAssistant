namespace LLMDesktopAssistant.StructuredValues
{
	public interface INodeValue
	{
		/// <summary>
		/// Get a snapshot of the current value of the parameter.
		/// This is used mostly for logging validation errors.
		/// </summary>
		/// <returns>A snapshot of the current value of the parameter.</returns>
		public object? TakeValueSnapshot();
	}
}
