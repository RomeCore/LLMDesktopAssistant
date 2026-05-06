using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Agents.ExecutionStages;
using LLMDesktopAssistant.LLM.Services.Agents;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.ExecutionStages;

/// <summary>
/// Common interface for all stage ViewModels.
/// </summary>
public abstract class StageViewModelBase : ViewModelBase
{
	/// <summary>
	/// The underlying model stage.
	/// </summary>
	public abstract AgentExecutionStage ModelStage { get; }

	public IAgentManagementService AgentManager { get; }

	public StageViewModelBase(IAgentManagementService agentManager)
	{
		AgentManager = agentManager;
	}
}
