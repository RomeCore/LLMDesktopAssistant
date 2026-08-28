namespace LLMDesktopAssistant.Prompting.Parameterization
{
	public readonly struct ParameterSchemaPath
	{
		public static ParameterSchemaPath Root { get; } = new ParameterSchemaPath("$root");

		public readonly string Value { get; }

		public ParameterSchemaPath(string value)
		{
			Value = value;
		}

		public ParameterSchemaPath Append(string path)
		{
			return new ParameterSchemaPath($"{Value}.{path}");
		}
	}
}
