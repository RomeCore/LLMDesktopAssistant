using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Tools.Specifiers;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.Agents;

/// <summary>
/// ViewModel for a single specifier rule row in the tool settings:
/// the pattern, the decision and the remove command.
/// </summary>
public class ToolSpecifierRuleViewModel : ViewModelBase
{
	private readonly ToolItemViewModel _owner;
	private readonly ToolSpecifierRule _rule;
	private SpecifierDecisionItem? _decision;

	/// <summary>
	/// Gets or sets the specifier pattern. Changes are persisted to the tool change immediately.
	/// </summary>
	public string Pattern
	{
		get => _rule.Pattern;
		set
		{
			if (_rule.Pattern == value)
				return;
			_rule.Pattern = value;
			_owner.SyncSpecifiers();
			RaisePropertyChanged();
		}
	}

	/// <summary>
	/// Gets all available specifier decisions with localized display names.
	/// </summary>
	public ImmutableList<SpecifierDecisionItem> Decisions { get; } = SpecifierDecisionItem.All;

	/// <summary>
	/// Gets or sets the decision applied when the pattern matches. Changes are persisted to the tool change immediately.
	/// </summary>
	public SpecifierDecisionItem? Decision
	{
		get => _decision;
		set
		{
			if (value == null || _decision?.Value == value.Value)
				return;
			_decision = value;
			_rule.Decision = value.Value;
			_owner.SyncSpecifiers();
			RaisePropertyChanged();
		}
	}

	/// <summary>
	/// Gets the command that removes this specifier rule from the tool.
	/// </summary>
	public ICommand RemoveCommand { get; }

	public ToolSpecifierRuleViewModel(ToolItemViewModel owner, ToolSpecifierRule rule)
	{
		_owner = owner;
		_rule = rule;
		_decision = Decisions.FirstOrDefault(i => i.Value == rule.Decision);
		RemoveCommand = new RelayCommand(() => owner.RemoveSpecifier(this));
	}
}
