using System.Globalization;

namespace LLMDesktopAssistant.Addons
{
	public class AddonDiagnostic<TCode>
		where TCode : Enum
	{
		public required bool IsFatal { get; init; }

		public required TCode Codes { get; init; }

		public ImmutableList<string> Messages { get; init; } = [];

		public Exception? Exception { get; init; } = null;

		public static AddonDiagnostic<TCode>? Combine(AddonDiagnostic<TCode>? first, AddonDiagnostic<TCode>? second)
		{
			if (first == null)
				return second;
			if (second == null)
				return first;

			return new AddonDiagnostic<TCode>
			{
				IsFatal = first.IsFatal || second.IsFatal,
				Codes = (TCode)Convert.ChangeType(Convert.ToUInt64(first.Codes) | Convert.ToUInt64(second.Codes),
					typeof(TCode), CultureInfo.InvariantCulture),
				Messages = [.. first.Messages, .. second.Messages],
				Exception = second.Exception ?? first.Exception
			};
		}

		public static TDiagnostic? Combine<TDiagnostic>(TDiagnostic? first, TDiagnostic? second)
			where TDiagnostic : AddonDiagnostic<TCode>, new()
		{
			if (first == null)
				return second;
			if (second == null)
				return first;

			return new TDiagnostic
			{
				IsFatal = first.IsFatal || second.IsFatal,
				Codes = (TCode)Convert.ChangeType(Convert.ToUInt64(first.Codes) | Convert.ToUInt64(second.Codes),
					typeof(TCode), CultureInfo.InvariantCulture),
				Messages = [.. first.Messages, .. second.Messages],
				Exception = second.Exception ?? first.Exception
			};
		}
	}

	public class AddonDiagnostic : AddonDiagnostic<AddonDiagnosticCode>
	{
	}
}
