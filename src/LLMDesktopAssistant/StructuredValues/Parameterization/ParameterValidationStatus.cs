namespace LLMDesktopAssistant.StructuredValues.Parameterization
{
	public enum ParameterValidationStatus
	{
		/// <summary>
		/// The value was created (initially was undefined or null).
		/// </summary>
		Created,

		/// <summary>
		/// The value was fixed. For sliders - clamped to the slider range or rounded if value requires integer.
		/// </summary>
		Fixed,

		/// <summary>
		/// The value was invalid (string or non-numeric value for numeric parameters) and was replaced with new value.
		/// </summary>
		Invalid
	}
}
