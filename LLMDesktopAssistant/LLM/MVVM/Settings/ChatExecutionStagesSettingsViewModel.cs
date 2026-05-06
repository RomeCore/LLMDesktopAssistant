using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Agents.ExecutionStages;
using LLMDesktopAssistant.LLM.MVVM.Settings.ExecutionStages;
using LLMDesktopAssistant.LLM.Services.Agents;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Utils;
using System.Text.Json;

namespace LLMDesktopAssistant.LLM.MVVM.Settings
{
	/// <summary>
	/// ViewModel for the Execution Stages settings tab.
	/// Manages a list of execution stages, each wrapping agents and execution logic.
	/// </summary>
	[ViewModelFor(typeof(ChatExecutionStagesSettingsView))]
	public class ChatExecutionStagesSettingsViewModel : ViewModelBase
	{
		public ChatAgentSettings AgentSettings { get; }
		public IAgentManagementService AgentManager { get; }

		/// <summary>
		/// Available stage types for the ComboBox in the header.
		/// </summary>
		public List<StageTypeOption> AvailableStageTypes { get; } =
		[
			StageTypeOption.Sequential,
			StageTypeOption.Random,
			StageTypeOption.MentionOnly,
			StageTypeOption.Adaptive
		];

		public RangeObservableCollection<StageContainerViewModel> Stages { get; } = [];

		public IRelayCommand AddStageCommand { get; }



		public ChatExecutionStagesSettingsViewModel(
			ChatAgentSettings agentSettings,
			IAgentManagementService agentManager)
		{
			AgentSettings = agentSettings;
			AgentManager = agentManager;

			AddStageCommand = new RelayCommand(AddStage);

			AgentSettings.EnsureDefaultAgent();
			RefreshStages();
		}

		public void RefreshStages()
		{
			Stages.Clear();
			foreach (var stage in AgentSettings.ExecutionStages)
			{
				Stages.Add(CreateContainer(stage));
			}
		}

		public StageContainerViewModel CreateContainer(AgentExecutionStage stage)
		{
			var container = new StageContainerViewModel(
				this,
				stage,
				AvailableStageTypes);

			return container;
		}

		private void AddStage()
		{
			var stage = new SequentialAgentExecutionStage
			{
				Id = Guid.NewGuid(),
				Enabled = true
			};

			// Add first available agent if any
			var agents = GetAllAgents().ToList();
			if (agents.Count > 0)
			{
				stage.AgentInstances.Add(new AgentInstance
				{
					AgentId = agents[0].Id,
					Enabled = true
				});
			}

			AgentSettings.ExecutionStages.Add(stage);
			Stages.Add(CreateContainer(stage));
		}

		public IEnumerable<AgentDescriptor> GetAllAgents()
		{
			return AgentManager.ListAgents().Select(a => a.Agent);
		}
	}
}
