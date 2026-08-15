using System;
using System.Collections.Generic;
using System.Text;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Prompting.Skills;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	public interface ISkillsetBuildingService
	{
		/// <summary>
		/// Returns all skills available in the current session.
		/// </summary>
		/// <returns>A list of <see cref="SkillInfo"/> objects representing all skills.</returns>
		IEnumerable<SkillInfo> GetAvailableSkills();

		/// <summary>
		/// Returns all skills available for a specific agent in the current session.
		/// </summary>
		/// <param name="agent">The agent for which to retrieve skills.</param>
		/// <returns>A list of <see cref="SkillInfo"/> objects representing all skills available for the specified agent.</returns>
		IEnumerable<SkillInfo> GetSkillsForAgent(ChatAgentDescriptor agent);
	}
}