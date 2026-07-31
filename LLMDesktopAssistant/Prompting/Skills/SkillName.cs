using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Prompting.Skills
{
	public static class SkillName
	{
		public static bool IsValidSkillName(string skillName)
		{
			if (string.IsNullOrWhiteSpace(skillName))
				return false;

			char p = '\0';
			for (int i = 0; i < skillName.Length; i++)
			{
				char c = skillName[i];

				// Only digits, lowercase latin characters and hyphens are allowed
				if (
					!(c >= '0' && c <= '9') &&
					!(c >= 'a' && c <= 'z') &&
					c != '-'
					)
					return false;

				// Disallow back-to-back hyphens
				if (p == '-' && c == '-')
					return false;

				p = c;
			}

			// Cannot start and end with hyphens
			if (skillName.StartsWith('-') || skillName.EndsWith('-'))
				return false;

			return true;
		}

		public static string ToValidSkillName(string skillName)
		{
			return skillName.Slugify().Trim().Trim('-');
		}
	}
}
