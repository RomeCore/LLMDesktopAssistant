using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Agents.SubAgents
{
	/// <summary>
	/// Provides validation and normalization helpers for sub-agent names.
	/// </summary>
	public static class SubAgentName
	{
		/// <summary>
		/// Determines whether the specified name is a valid sub-agent name.
		/// </summary>
		/// <param name="subAgentName">The sub-agent name to validate.</param>
		/// <returns><see langword="true"/> if the name is valid; otherwise, <see langword="false"/>.</returns>
		public static bool IsValidSubAgentName(string subAgentName)
		{
			// Length must be in range of 1-64
			if (string.IsNullOrWhiteSpace(subAgentName))
				return false;
			if (subAgentName.Length > 64)
				return false;

			char p = '\0';
			for (int i = 0; i < subAgentName.Length; i++)
			{
				char c = subAgentName[i];

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
			if (subAgentName.StartsWith('-') || subAgentName.EndsWith('-'))
				return false;

			return true;
		}

		/// <summary>
		/// Converts the specified name to a valid sub-agent name.
		/// </summary>
		/// <param name="subAgentName">The sub-agent name to normalize.</param>
		/// <returns>A valid sub-agent name.</returns>
		public static string ToValidSubAgentName(string subAgentName)
		{
			subAgentName = subAgentName.Slugify().Trim().Trim('-');
			if (subAgentName.Length > 64)
				return subAgentName.Substring(0, 64);
			return subAgentName;
		}
	}
}
