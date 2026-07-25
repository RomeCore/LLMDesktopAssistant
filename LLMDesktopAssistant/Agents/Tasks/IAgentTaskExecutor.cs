using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Agents.Tasks
{
	public interface IAgentTaskExecutor
	{
		AgentTask Execute(AgentTaskLaunchParameters parameters, CancellationToken cancellationToken = default);
	}
}
