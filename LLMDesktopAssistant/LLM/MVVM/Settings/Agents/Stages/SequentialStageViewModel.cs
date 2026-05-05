using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Agents.ExecutionStages;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.Agents.Stages;

[ViewModelFor(typeof(SequentialStageView))]
public class SequentialStageViewModel : StageViewModelBase
{
	public override AgentExecutionStage ModelStage { get; }
	public SequentialAgentExecutionStage SequentialStage => (SequentialAgentExecutionStage)ModelStage;

	public RangeObservableCollection<SequentialStageAgentViewModel> Agents { get; } = [];

	public IRelayCommand AddAgentCommand { get; }

	public SequentialStageViewModel(SequentialAgentExecutionStage stage, IAgentManagementService agentManager) : base(agentManager)
	{
		ModelStage = stage;

		AddAgentCommand = new RelayCommand(AddAgent);

		Agents.Clear();
		foreach (var instance in SequentialStage.AgentInstances)
		{
			var agent = FindAgentDescriptor(instance.AgentId);
			if (agent == null) continue;

			Agents.Add(new SequentialStageAgentViewModel(
				onMoveUp: vm => MoveAgent(vm, -1),
				onMoveDown: vm => MoveAgent(vm, 1),
				onRemove: vm => RemoveAgent(vm))
			{
				Agent = agent,
				Instance = instance
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
			.Where(a => !SequentialStage.AgentInstances.Any(ai => ai.AgentId == a.Id))
			.ToList();

		if (available == null || available.Count == 0) return;

		var agent = available[0];
		var instance = new AgentInstance
		{
			AgentId = agent.Id,
			Enabled = true
		};
		SequentialStage.AgentInstances.Add(instance);

		Agents.Add(new SequentialStageAgentViewModel(
			onMoveUp: vm => MoveAgent(vm, -1),
			onMoveDown: vm => MoveAgent(vm, 1),
			onRemove: vm => RemoveAgent(vm))
		{
			Agent = agent,
			Instance = instance
		});
	}

	private void RemoveAgent(SequentialStageAgentViewModel vm)
	{
		var idx = Agents.IndexOf(vm);
		if (idx < 0) return;

		Agents.RemoveAt(idx);
		SequentialStage.AgentInstances.RemoveAt(idx);
	}

	private void MoveAgent(SequentialStageAgentViewModel vm, int direction)
	{
		var idx = Agents.IndexOf(vm);
		if (idx < 0) return;
		var newIdx = idx + direction;
		if (newIdx < 0 || newIdx >= Agents.Count) return;

		Agents.Move(idx, newIdx);
		SequentialStage.AgentInstances.Move(idx, newIdx);
	}
}
