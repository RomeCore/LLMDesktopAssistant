using LLMDesktopAssistant.Agents.ExecutionStages;
using LLMDesktopAssistant.LLM.Services;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.Agents.Stages;

/// <summary>
/// Default implementation of <see cref="IStageViewModelFactory"/>.
/// Creates the appropriate ViewModel based on the stage type.
/// </summary>
public static class StageViewModelFactory
{
	public static StageViewModelBase CreateViewModel(AgentExecutionStage stage, IAgentManagementService agentManager)
	{
		return stage switch
		{
			SequentialAgentExecutionStage sequential => new SequentialStageViewModel(sequential, agentManager),
			RandomAgentExecutionStage random => new RandomStageViewModel(random, agentManager),
			_ => throw new ArgumentException($"Unknown stage type: {stage.GetType()}")
		};
	}
}
