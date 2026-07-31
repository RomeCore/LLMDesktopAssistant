using LLMDesktopAssistant.Services;
using Serilog;

namespace LLMDesktopAssistant.Prompting.Skills
{
	[Service(typeof(ISkillLoader))]
	public class SkillLoader(
		ISkillParser parser
	) : ISkillLoader
	{
		public IEnumerable<SkillInfo> Load(IEnumerable<string> files)
		{
			foreach (var file in files)
			{
				Exception? exception = null;
				var fallbackSkillName = Path.GetFileName(Path.GetDirectoryName(file));
				var nameUnknownCode = string.IsNullOrEmpty(fallbackSkillName)
					? SkillDiagnosticCode.MissingName
					: SkillDiagnosticCode.None;
				fallbackSkillName = string.IsNullOrEmpty(fallbackSkillName) ? "unknown" : fallbackSkillName;

				if (!File.Exists(file))
				{
					yield return CreateDiagnosticSkill(fallbackSkillName, new SkillDiagnostic
					{
						IsFatal = true,
						Codes = SkillDiagnosticCode.MissingFile | nameUnknownCode,
						Exception = null
					});
					continue;
				}

				string? contents = null;
				try
				{
					contents = File.ReadAllText(file);
				}
				catch (Exception ex)
				{
					exception = ex;
				}

				if (exception != null)
				{
					yield return CreateDiagnosticSkill(fallbackSkillName, new SkillDiagnostic
					{
						IsFatal = true,
						Codes = SkillDiagnosticCode.FileAccessError | nameUnknownCode,
						Exception = exception
					});
					continue;
				}

				SkillInfo? parsed = null;
				try
				{
					parsed = parser.Parse(file, contents!);
				}
				catch (Exception ex)
				{
					exception = ex;
				}

				if (exception != null)
				{
					yield return CreateDiagnosticSkill(fallbackSkillName, new SkillDiagnostic
					{
						IsFatal = true,
						Codes = SkillDiagnosticCode.GeneralParsingError | nameUnknownCode,
						Exception = exception
					});
					continue;
				}

				if (parsed != null)
					yield return parsed;
			}
		}

		private static SkillInfo CreateDiagnosticSkill(string name, SkillDiagnostic diagnostic)
		{
			return new SkillInfo
			{
				Name = name,
				Description = string.Empty,
				Body = string.Empty,
				Diagnostic = diagnostic
			};
		}
	}
}
