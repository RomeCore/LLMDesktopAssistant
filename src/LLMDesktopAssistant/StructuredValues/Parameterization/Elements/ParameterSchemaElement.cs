using LLMDesktopAssistant.StructuredValues.Reactive;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.StructuredValues.Parameterization.Elements
{
	/// <summary>
	/// Base class for parameter schema elements.
	/// </summary>
	public abstract class ParameterSchemaElement : NotifyPropertyChanged
	{
		public string? Title
		{
			get;
			set => SetProperty(ref field, value);
		}

		public string? Description
		{
			get;
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// Creates or validates a value based on the schema element.
		/// If the value is null, it creates a new one.
		/// If not valid - tries to fix value, or creates a new one if fixing is not possible.
		/// </summary>
		/// <param name="existing">The existing value to validate or fix.</param>
		/// <param name="log">A list to log validation or fixing messages.</param>
		/// <returns>The created or fixed value.</returns>
		public abstract ReactiveNodeValue CreateOrFixValue(ReactiveNodeValue? existing, AppendOnlyList<ParameterValidationLogEntry> log);
	}
}
