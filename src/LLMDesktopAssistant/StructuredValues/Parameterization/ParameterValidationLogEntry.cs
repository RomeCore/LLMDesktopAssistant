namespace LLMDesktopAssistant.StructuredValues.Parameterization
{
	public class ParameterValidationLogEntry
	{
		/// <summary>
		/// The validation status of the parameter value.
		/// </summary>
		public required ParameterValidationStatus Status { get; init; }

		/// <summary>
		/// The original value that was replaced or fixed.
		/// </summary>
		public object? OriginalValue { get; init; }

		/// <summary>
		/// The fixed or created value. Null if the value was not created or fixed.
		/// </summary>
		public object? FinalValue { get; init; }

		/// <summary>
		/// A message describing the validation process.
		/// </summary>
		public string? Message { get; init; }
	}
}
