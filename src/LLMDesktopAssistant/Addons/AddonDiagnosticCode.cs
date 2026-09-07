namespace LLMDesktopAssistant.Addons
{
	public class AddonDiagnosticCode
	{
		protected AddonDiagnosticCode()
		{
		}

		public static AddonDiagnosticCodeValue None => 0;

		public static AddonDiagnosticCodeValue GeneralParsingError => 1 << 0;

		public static AddonDiagnosticCodeValue MissingFrontmatter => 1 << 1;

		public static AddonDiagnosticCodeValue FrontmatterParsingError => 1 << 2;

		public static AddonDiagnosticCodeValue FrontmatterDecodingError => 1 << 3;

		public static AddonDiagnosticCodeValue NameFSMismatch => 1 << 4;

		public static AddonDiagnosticCodeValue NameFormatError => 1 << 5;

		public static AddonDiagnosticCodeValue MissingName => 1 << 6;

		public static AddonDiagnosticCodeValue MissingFrontmatterName => 1 << 7;

		public static AddonDiagnosticCodeValue MissingDescription => 1 << 8;

		public static AddonDiagnosticCodeValue MissingFrontmatterDescription => 1 << 9;

		public static AddonDiagnosticCodeValue MissingFile => 1 << 10;

		public static AddonDiagnosticCodeValue FileAccessError => 1 << 11;
	}
}
