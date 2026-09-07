namespace LLMDesktopAssistant.Addons
{
	public class AddonDiagnostic
	{
		public bool IsFatal { get; init; }

		public AddonDiagnosticCode Codes { get; init; }

		public ImmutableList<string> Messages { get; init; } = [];

		public ImmutableList<Exception> Exceptions { get; init; } = [];

		public static AddonDiagnostic? Combine(AddonDiagnostic? first, AddonDiagnostic? second)
		{
			if (first == null)
				return second;
			if (second == null)
				return first;

			return new AddonDiagnostic
			{
				IsFatal = first.IsFatal || second.IsFatal,
				Codes = first.Codes | second.Codes,
				Messages = [.. first.Messages, .. second.Messages],
				Exceptions = [.. first.Exceptions, .. second.Exceptions]
			};
		}
	}

	public static class AddonDiagnosticExtensions
	{
		extension(AddonDiagnostic? diagnostic)
		{
			public AddonDiagnostic? Combine(AddonDiagnostic? other)
			{
				return AddonDiagnostic.Combine(diagnostic, other);
			}
		}
	}
}
