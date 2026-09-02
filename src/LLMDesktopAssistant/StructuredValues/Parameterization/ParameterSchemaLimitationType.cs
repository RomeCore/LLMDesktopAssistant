namespace LLMDesktopAssistant.StructuredValues.Parameterization
{
	[Flags]
	public enum ParameterSchemaLimitationType
	{
		None = 0,

		NotSpecified = 1 << 0,

		Boolean = 1 << 1,

		Integer = 1 << 2,

		Number = 1 << 3,

		String = 1 << 4,

		Object = 1 << 5
	}
}
