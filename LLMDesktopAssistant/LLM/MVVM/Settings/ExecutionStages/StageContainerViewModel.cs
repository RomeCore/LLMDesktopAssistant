using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Agents.ExecutionStages;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Utils;
using System.Text.Json;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.ExecutionStages;

/// <summary>
/// Represents the available stage type in the UI combobox.
/// </summary>
public sealed class StageTypeOption
{
	public required string Name { get; init; }
	public string DisplayName => LocalizationManager.LocalizeStatic(Name);
	public required Type StageType { get; init; }

	public static StageTypeOption Sequential { get; } = new StageTypeOption
	{
		Name = "stage_type_sequential",
		StageType = typeof(SequentialAgentExecutionStage)
	};
	public static StageTypeOption Random { get; } = new StageTypeOption
	{
		Name = "stage_type_random",
		StageType = typeof(RandomAgentExecutionStage)
	};
	public static StageTypeOption MentionOnly { get; } = new StageTypeOption
	{
		Name = "stage_type_mention_only",
		StageType = typeof(MentionOnlyAgentExecutionStage)
	};
	public static StageTypeOption Adaptive { get; } = new StageTypeOption
	{
		Name = "stage_type_adaptive",
		StageType = typeof(AdaptiveAgentExecutionStage)
	};
	public static StageTypeOption RoundRobin { get; } = new StageTypeOption
	{
		Name = "stage_type_round_robin",
		StageType = typeof(RoundRobinAgentExecutionStage)
	};

	public static StageTypeOption[] All { get; } = new[] { Sequential, Random, MentionOnly, Adaptive, RoundRobin };
}

/// <summary>
/// Wrapper ViewModel for any execution stage.
/// Handles the header UI: type selection, move up/down, delete, clone.
/// Delegates the body content to the stage-specific <see cref="IStageViewModel"/>.
/// </summary>
public class StageContainerViewModel : ViewModelBase
{
	private readonly ChatExecutionStagesSettingsViewModel _parent;

	private AgentExecutionStage _stage;
	public AgentExecutionStage Stage
	{
		get => _stage;
		set => SetProperty(ref _stage, value);
	}

	private StageViewModelBase _stageVM;
	public StageViewModelBase StageVM
	{
		get => _stageVM;
		set => SetProperty(ref _stageVM, value);
	}

	public StageTypeOption[] AvailableStageTypes => StageTypeOption.All;

	private StageTypeOption _selectedType;
	public StageTypeOption SelectedType
	{
		get => _selectedType;
		set
		{
			if (SetProperty(ref _selectedType, value))
			{
				OnTypeChanged();
			}
		}
	}

	public IRelayCommand MoveUpCommand { get; }
	public IRelayCommand MoveDownCommand { get; }
	public IRelayCommand DeleteCommand { get; }
	public IRelayCommand CloneCommand { get; }

	public StageContainerViewModel(
		ChatExecutionStagesSettingsViewModel parent,
		AgentExecutionStage stage)
	{
		_parent = parent;
		_stage = stage;
		_stageVM = StageViewModelFactory.CreateViewModel(Stage, _parent.AgentManager);
		_selectedType = stage switch
		{
			SequentialAgentExecutionStage => StageTypeOption.Sequential,
			RandomAgentExecutionStage => StageTypeOption.Random,
			MentionOnlyAgentExecutionStage => StageTypeOption.MentionOnly,
			AdaptiveAgentExecutionStage => StageTypeOption.Adaptive,
			RoundRobinAgentExecutionStage => StageTypeOption.RoundRobin,
			_ => StageTypeOption.Sequential
		};

		MoveUpCommand = new RelayCommand<StageContainerViewModel>(_ => MoveStage(-1));
		MoveDownCommand = new RelayCommand<StageContainerViewModel>(_ => MoveStage(+1));
		DeleteCommand = new RelayCommand<StageContainerViewModel>(_ => RemoveStage());
		CloneCommand = new RelayCommand<StageContainerViewModel>(_ => CloneStage());
	}

	private void RemoveStage()
	{
		var idx = _parent.Stages.IndexOf(this);
		if (idx < 0) return;

		_parent.Stages.RemoveAt(idx);
		_parent.AgentSettings.ExecutionStages.RemoveAt(idx);
	}

	private void CloneStage()
	{
		var idx = _parent.Stages.IndexOf(this);
		if (idx < 0) return;

		var json = JsonSerializer.Serialize(Stage);
		var clone = JsonSerializer.Deserialize<AgentExecutionStage>(json);
		if (clone == null) return;

		clone.Id = Guid.NewGuid();

		_parent.AgentSettings.ExecutionStages.Insert(idx + 1, clone);
		_parent.Stages.Insert(idx + 1, _parent.CreateContainer(clone));
	}

	private void MoveStage(int direction)
	{
		var idx = _parent.Stages.IndexOf(this);
		if (idx < 0) return;
		var newIdx = idx + direction;
		if (newIdx < 0 || newIdx >= _parent.Stages.Count) return;

		_parent.Stages.Move(idx, newIdx);
		_parent.AgentSettings.ExecutionStages.Move(idx, newIdx);
	}

	private void OnTypeChanged()
	{
		var prevStage = Stage;
		var prevRepeatable = prevStage as RepeatableBaseAgentExecutionStage;
		var prevMentionable = prevStage as MentionableBaseAgentExecutionStage;
		var prevIndex = _parent.AgentSettings.ExecutionStages.IndexOf(prevStage);

		if (SelectedType.StageType == typeof(SequentialAgentExecutionStage))
		{
			Stage = new SequentialAgentExecutionStage
			{
				Id = Stage.Id,
				Enabled = Stage.Enabled,
				AgentInstances = Stage.AgentInstances
			};
		}
		else if (SelectedType.StageType == typeof(RandomAgentExecutionStage))
		{
			Stage = new RandomAgentExecutionStage
			{
				Id = Stage.Id,
				Enabled = Stage.Enabled,
				AgentInstances = Stage.AgentInstances,
				EnableMentions = prevMentionable?.EnableMentions ?? true,
				CanAgentsExecuteAgain = prevRepeatable?.CanAgentsExecuteAgain ?? false,
				MinIterations = prevRepeatable?.MinIterations ?? -1,
				MaxIterations = prevRepeatable?.MaxIterations ?? -1,
				StopChance = prevRepeatable?.StopChance ?? 0.0,
			};
		}
		else if (SelectedType.StageType == typeof(MentionOnlyAgentExecutionStage))
		{
			Stage = new MentionOnlyAgentExecutionStage
			{
				Id = Stage.Id,
				Enabled = Stage.Enabled,
				AgentInstances = Stage.AgentInstances,
				EnableMentions = true,
				CanAgentsExecuteAgain = prevRepeatable?.CanAgentsExecuteAgain ?? false,
				MinIterations = prevRepeatable?.MinIterations ?? -1,
				MaxIterations = prevRepeatable?.MaxIterations ?? -1,
				StopChance = prevRepeatable?.StopChance ?? 0.0,
			};
		}
		else if (SelectedType.StageType == typeof(AdaptiveAgentExecutionStage))
		{
			Stage = new AdaptiveAgentExecutionStage
			{
				Id = Stage.Id,
				Enabled = Stage.Enabled,
				AgentInstances = Stage.AgentInstances,
				EnableMentions = prevMentionable?.EnableMentions ?? true,
				CanAgentsExecuteAgain = prevRepeatable?.CanAgentsExecuteAgain ?? false,
				MinIterations = prevRepeatable?.MinIterations ?? -1,
				MaxIterations = prevRepeatable?.MaxIterations ?? -1,
				StopChance = prevRepeatable?.StopChance ?? 0.0,
			};
		}
		else if (SelectedType.StageType == typeof(RoundRobinAgentExecutionStage))
		{
			Stage = new RoundRobinAgentExecutionStage
			{
				Id = Stage.Id,
				Enabled = Stage.Enabled,
				AgentInstances = [.. Stage.AgentInstances.Select(i => {
					i.Weight = Math.Clamp(i.Weight, 0.0, 1.0);
					return i;
				}) ],
				CanAgentsBeSkipped = false,
				MinCycles = -1,
				MaxCycles = -1,
				StopChance = 0.0,
			};
		}
		else
		{
			throw new ArgumentOutOfRangeException(nameof(SelectedType.StageType));
		}

		if (prevIndex != -1)
			_parent.AgentSettings.ExecutionStages[prevIndex] = Stage;

		StageVM = StageViewModelFactory.CreateViewModel(Stage, _parent.AgentManager);
	}
}
