using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Agents.ExecutionStages;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.Agents.Stages;

public abstract class StageAgentItemViewModelBase : NotifyPropertyChanged
{
	public required AgentDescriptor Agent { get; init; }
	public required AgentInstance Instance { get; init; }
}

/// <summary>
/// ViewModel for an agent inside a sequential stage.
/// </summary>
public class SequentialStageAgentViewModel : StageAgentItemViewModelBase
{
	public IRelayCommand MoveUpCommand { get; }
	public IRelayCommand MoveDownCommand { get; }
	public IRelayCommand RemoveCommand { get; }

	public SequentialStageAgentViewModel(
		Action<SequentialStageAgentViewModel> onMoveUp,
		Action<SequentialStageAgentViewModel> onMoveDown,
		Action<SequentialStageAgentViewModel> onRemove)
	{
		MoveUpCommand = new RelayCommand<SequentialStageAgentViewModel>(_ => onMoveUp(this));
		MoveDownCommand = new RelayCommand<SequentialStageAgentViewModel>(_ => onMoveDown(this));
		RemoveCommand = new RelayCommand<SequentialStageAgentViewModel>(_ => onRemove(this));
	}
}

/// <summary>
/// ViewModel for an agent inside a random stage.
/// </summary>
public class RandomStageAgentViewModel : StageAgentItemViewModelBase
{
	public required WeightedAgentInstance WeightedInstance { get; init; }
	public IRelayCommand RemoveCommand { get; }

	public RandomStageAgentViewModel(
		Action<RandomStageAgentViewModel> onRemove)
	{
		RemoveCommand = new RelayCommand<RandomStageAgentViewModel>(_ => onRemove(this));
	}
}
