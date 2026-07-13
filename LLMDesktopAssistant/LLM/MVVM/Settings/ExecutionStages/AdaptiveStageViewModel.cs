using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Agents.ExecutionStages;
using LLMDesktopAssistant.LLM.Services.Agents;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.ExecutionStages;

[ViewModelFor(typeof(AdaptiveStageView))]
public class AdaptiveStageViewModel : StageViewModelBase
{
	public override AgentExecutionStage ModelStage { get; }
	public AdaptiveAgentExecutionStage AdaptiveStage => (AdaptiveAgentExecutionStage)ModelStage;

	public RangeObservableCollection<MentionStageAgentViewModel> Agents { get; } = [];

	public IRelayCommand AddAgentCommand { get; }

	public AdaptiveStageViewModel(AdaptiveAgentExecutionStage stage, IAgentManagementService agentManager) : base(agentManager)
	{
		ModelStage = stage;

		AddAgentCommand = new RelayCommand(AddAgent);

		Agents.Clear();
		foreach (var instance in AdaptiveStage.AgentInstances)
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

	private ChatAgentDescriptor? FindAgentDescriptor(Guid agentId)
	{
		return AgentManager.TryGetAgentDescriptor(agentId);
	}

	private void AddAgent()
	{
		var available = AgentManager.ListAgents().Select(a => a.Agent)
			.Where(a => !AdaptiveStage.AgentInstances.Any(ai => ai.AgentId == a.Id))
			.ToList();

		if (available == null || available.Count == 0) return;

		var agent = available[0];
		var instance = new ChatAgentInstance
		{
			AgentId = agent.Id,
			Enabled = true
		};
		AdaptiveStage.AgentInstances.Add(instance);

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
		AdaptiveStage.AgentInstances.RemoveAt(idx);
	}
}
