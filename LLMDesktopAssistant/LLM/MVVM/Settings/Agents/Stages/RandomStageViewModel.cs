using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Agents.ExecutionStages;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.Agents.Stages;

/// <summary>
/// ViewModel for a random execution stage.
/// Agents are selected randomly with weights.
/// </summary>
public class RandomStageViewModel : StageViewModelBase
{
	public override AgentExecutionStage ModelStage { get; }
	public AgentExecutionRandomStage RandomStage => (AgentExecutionRandomStage)ModelStage;

	public RangeObservableCollection<RandomStageAgentViewModel> Agents { get; } = [];

	public IRelayCommand AddAgentCommand { get; }

	public RandomStageViewModel(AgentExecutionRandomStage stage, IAgentManagementService agentManager) : base(agentManager)
	{
		ModelStage = stage;

		AddAgentCommand = new RelayCommand(AddAgent);

		Agents.Clear();
		foreach (var instance in RandomStage.AgentInstances)
		{
			var agent = FindAgentDescriptor(instance.AgentId);
			if (agent == null) continue;

			Agents.Add(new RandomStageAgentViewModel(vm => RemoveAgent(vm))
			{
				Agent = agent,
				Instance = instance,
				WeightedInstance = instance
			});
		}
	}

	private AgentDescriptor? FindAgentDescriptor(Guid agentId)
	{
		return AgentManager.TryGetAgentDescriptor(agentId);
	}

	private void AddAgent()
	{
		var available = AgentManager.ListAgents().Select(a => a.Agent)
			.Where(a => !RandomStage.AgentInstances.Any(ai => ai.AgentId == a.Id))
			.ToList();

		if (available == null || available.Count == 0) return;

		var agent = available[0];
		var instance = new WeightedAgentInstance
		{
			AgentId = agent.Id,
			Enabled = true,
			Weight = 1.0
		};
		RandomStage.AgentInstances.Add(instance);

		Agents.Add(new RandomStageAgentViewModel(vm => RemoveAgent(vm))
		{
			Agent = agent,
			Instance = instance,
			WeightedInstance = instance
		});
	}

	private void RemoveAgent(RandomStageAgentViewModel vm)
	{
		var idx = Agents.IndexOf(vm);
		if (idx < 0) return;

		Agents.RemoveAt(idx);
		RandomStage.AgentInstances.RemoveAt(idx);
	}
}
