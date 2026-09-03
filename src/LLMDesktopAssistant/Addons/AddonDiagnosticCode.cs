namespace LLMDesktopAssistant.Addons
{
	[Flags]
	public enum AddonDiagnosticCode
	{
		None = 0,

		MissingYaml = 1 << 0,

		YamlParsingError = 1 << 1,

		YamlDecodingError = 1 << 2,

		NameFSMismatch = 1 << 3,

		NameFormatError = 1 << 4,

		MissingName = 1 << 5,

		MissingYamlName = 1 << 6,

		MissingDescription = 1 << 7,

		MissingYamlDescription = 1 << 8,

		MissingFile = 1 << 9,

		FileAccessError = 1 << 10,

		GeneralParsingError = 1 << 11
	}
}
