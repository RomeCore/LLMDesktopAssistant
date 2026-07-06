using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Agents.ExecutionStages;
using LLMDesktopAssistant.LLM.Services.Agents;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.ExecutionStages;

/// <summary>
/// ViewModel for the round-robin execution stage.
/// </summary>
[ViewModelFor(typeof(RoundRobinStageView))]
public class RoundRobinStageViewModel : StageViewModelBase
{
	/// <inheritdoc />
	public override AgentExecutionStage ModelStage { get; }

	/// <summary>
	/// Gets the underlying round-robin execution stage model.
	/// </summary>
	public RoundRobinAgentExecutionStage RoundRobinStage => (RoundRobinAgentExecutionStage)ModelStage;

	/// <summary>
	/// Gets the collection of agent items displayed in the round-robin stage.
	/// </summary>
	public RangeObservableCollection<RoundRobinStageAgentViewModel> Agents { get; } = [];

	/// <summary>
	/// Command to add a new agent to the round-robin stage.
	/// </summary>
	public IRelayCommand AddAgentCommand { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="RoundRobinStageViewModel"/> class.
	/// </summary>
	/// <param name="stage">The round-robin execution stage model.</param>
	/// <param name="agentManager">The agent management service.</param>
	public RoundRobinStageViewModel(RoundRobinAgentExecutionStage stage, IAgentManagementService agentManager) : base(agentManager)
	{
		ModelStage = stage;

		AddAgentCommand = new RelayCommand(AddAgent);

		Agents.Clear();
		foreach (var instance in RoundRobinStage.AgentInstances)
		{
			var agent = FindAgentDescriptor(instance.AgentId);
			if (agent == null) continue;

			Agents.Add(new RoundRobinStageAgentViewModel(
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
			.Where(a => !RoundRobinStage.AgentInstances.Any(ai => ai.AgentId == a.Id))
			.ToList();

		if (available.Count == 0) return;

		var agent = available[0];
		var instance = new AgentInstance
		{
			AgentId = agent.Id,
			Enabled = true
		};
		RoundRobinStage.AgentInstances.Add(instance);

		Agents.Add(new RoundRobinStageAgentViewModel(
			onMoveUp: vm => MoveAgent(vm, -1),
			onMoveDown: vm => MoveAgent(vm, 1),
			onRemove: vm => RemoveAgent(vm))
		{
			Agent = agent,
			Instance = instance
		});
	}

	private void RemoveAgent(RoundRobinStageAgentViewModel vm)
	{
		var idx = Agents.IndexOf(vm);
		if (idx < 0) return;

		Agents.RemoveAt(idx);
		RoundRobinStage.AgentInstances.RemoveAt(idx);
	}

	private void MoveAgent(RoundRobinStageAgentViewModel vm, int direction)
	{
		var idx = Agents.IndexOf(vm);
		if (idx < 0) return;
		var newIdx = idx + direction;
		if (newIdx < 0 || newIdx >= Agents.Count) return;

		Agents.Move(idx, newIdx);
		RoundRobinStage.AgentInstances.Move(idx, newIdx);
	}
}
