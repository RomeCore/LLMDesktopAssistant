using LLMDesktopAssistant.Agents.Tasks;

namespace LLMDesktopAssistant.LLM.Services.Agents
{
	public interface ISubAgentTaskParamsResolver
	{
		AgentTaskLaunchParameters Resolve(AgentTaskLaunchParameters sourceParameters, TaskSubAgentDescriptor descriptor, IEnumerable<AgentChatMessage> additionalMessages, out List<string> errors);
	}
}