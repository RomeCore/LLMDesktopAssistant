using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Agents.ExecutionStages;
using LLMDesktopAssistant.LLM.Services.Agents;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.ExecutionStages;

[ViewModelFor(typeof(MentionOnlyStageView))]
public class MentionOnlyStageViewModel : StageViewModelBase
{
	public override AgentExecutionStage ModelStage { get; }
	public MentionOnlyAgentExecutionStage MentionStage => (MentionOnlyAgentExecutionStage)ModelStage;

	public RangeObservableCollection<MentionStageAgentViewModel> Agents { get; } = [];

	public IRelayCommand AddAgentCommand { get; }

	public MentionOnlyStageViewModel(MentionOnlyAgentExecutionStage stage, IAgentManagementService agentManager) : base(agentManager)
	{
		ModelStage = stage;

		AddAgentCommand = new RelayCommand(AddAgent);

		Agents.Clear();
		foreach (var instance in MentionStage.AgentInstances)
		{
			var agent = FindAgentDescriptor(instance.AgentId);
			if (agent == null) continue;

			Agents.Add(new MentionStageAgentViewModel(vm => RemoveAgent(vm))
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
			.Where(a => !MentionStage.AgentInstances.Any(ai => ai.AgentId == a.Id))
			.ToList();

		if (available == null || available.Count == 0) return;

		var agent = available[0];
		var instance = new AgentInstance
		{
			AgentId = agent.Id,
			Enabled = true
		};
		MentionStage.AgentInstances.Add(instance);

		Agents.Add(new MentionStageAgentViewModel(vm => RemoveAgent(vm))
		{
			Agent = agent,
			Instance = instance
		});
	}

	private void RemoveAgent(MentionStageAgentViewModel vm)
	{
		var idx = Agents.IndexOf(vm);
		if (idx < 0) return;

		Agents.RemoveAt(idx);
		MentionStage.AgentInstances.RemoveAt(idx);
	}
}
