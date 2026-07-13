using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Agents.ExecutionStages;
using LLMDesktopAssistant.LLM.Services.Agents;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.ExecutionStages;

[ViewModelFor(typeof(RandomStageView))]
public class RandomStageViewModel : StageViewModelBase
{
	public override AgentExecutionStage ModelStage { get; }
	public RandomAgentExecutionStage RandomStage => (RandomAgentExecutionStage)ModelStage;

	public RangeObservableCollection<RandomStageAgentViewModel> Agents { get; } = [];

	public IRelayCommand AddAgentCommand { get; }

	public RandomStageViewModel(RandomAgentExecutionStage stage, IAgentManagementService agentManager) : base(agentManager)
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
				Instance = instance
			});
		}
	}

	private ChatAgentDescriptor? FindAgentDescriptor(Guid agentId)
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
		var instance = new ChatAgentInstance
		{
			AgentId = agent.Id,
			Enabled = true,
			Weight = 1.0
		};
		RandomStage.AgentInstances.Add(instance);

		Agents.Add(new RandomStageAgentViewModel(vm => RemoveAgent(vm))
		{
			Agent = agent,
			Instance = instance
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
