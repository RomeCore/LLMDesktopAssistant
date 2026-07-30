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
				SkillInfo? parsed = null;
				try
				{
					var contents = File.ReadAllText(file);
					parsed = parser.Parse(file, contents);
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Failed to parse skill file '{File}': {Error}", file, ex);
				}
				if (parsed != null)
					yield return parsed;
			}
		}
	}
}
