using LLMDesktopAssistant.Agents.ExecutionStages;
using LLMDesktopAssistant.LLM.Services.Agents;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.ExecutionStages;

public static class StageViewModelFactory
{
	public static StageViewModelBase CreateViewModel(AgentExecutionStage stage, IAgentManagementService agentManager)
	{
		return stage switch
		{
			SequentialAgentExecutionStage sequential => new SequentialStageViewModel(sequential, agentManager),
			RandomAgentExecutionStage random => new RandomStageViewModel(random, agentManager),
			MentionOnlyAgentExecutionStage mention => new MentionOnlyStageViewModel(mention, agentManager),
			AdaptiveAgentExecutionStage adaptive => new AdaptiveStageViewModel(adaptive, agentManager),
			_ => throw new ArgumentException($"Unknown stage type: {stage.GetType()}")
		};
	}
}
