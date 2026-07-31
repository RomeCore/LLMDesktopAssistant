using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Prompting.Skills
{
	public static class SkillName
	{
		public static bool IsValidSkillName(string skillName)
		{
			// Length must be in range of 1-64
			if (string.IsNullOrWhiteSpace(skillName))
				return false;
			if (skillName.Length > 64)
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
			skillName = skillName.Slugify().Trim().Trim('-');
			if (skillName.Length > 64)
				return skillName.Substring(0, 64);
			return skillName;
		}
	}
}
