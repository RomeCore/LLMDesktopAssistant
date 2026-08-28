namespace LLMDesktopAssistant.Prompting
{
	[Flags]
	public enum PromptPartDiagnosticCode
	{
		None = 0,

		MissingTemplateIdentifier = 1 << 0,

		MissingGuid = 1 << 1,

		InvalidGuid = 1 << 2,

		MissingStrId = 1 << 3,

		MissingLanguage = 1 << 4,

		MissingName = 1 << 5,

		MissingDescription = 1 << 6,

		MissingCategory = 1 << 7,

		InvalidParameterSchema = 1 << 8,

		InvalidSlotKind = 1 << 9,
	}
}