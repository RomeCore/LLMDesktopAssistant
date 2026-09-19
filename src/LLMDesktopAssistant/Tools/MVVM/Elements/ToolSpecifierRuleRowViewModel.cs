using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.LLM.MVVM.Settings.Agents;
using LLMDesktopAssistant.Tools.Specifiers;

namespace LLMDesktopAssistant.Tools.MVVM.Elements
{
	/// <summary>
	/// The view model of a single specifier rule row of a tool card: the pattern, the decision and the
	/// remove command. Every change is persisted to the tool change immediately.
	/// </summary>
	public class ToolSpecifierRuleRowViewModel : NotifyPropertyChanged
	{
		private readonly AddonCardToolSpecifiersBlock _owner;
		private bool _enabled;
		private string _pattern;
		private SpecifierDecisionItem? _decision;

		/// <summary>
		/// Initializes a new instance of the <see cref="ToolSpecifierRuleRowViewModel"/> class.
		/// </summary>
		/// <param name="owner">The block the row belongs to.</param>
		/// <param name="pattern">The specifier pattern of the rule.</param>
		/// <param name="decision">The decision applied when the pattern matches.</param>
		public ToolSpecifierRuleRowViewModel(AddonCardToolSpecifiersBlock owner, bool enabled, string pattern, SpecifierDecision decision)
		{
			_owner = owner;
			_enabled = enabled;
			_pattern = pattern;
			_decision = Decisions.FirstOrDefault(item => item.Value == decision) ?? Decisions.FirstOrDefault();

			RemoveCommand = new RelayCommand(() => owner.RemoveRule(this));
		}

		public bool Enabled
		{
			get => _enabled;
			set
			{
				if (_enabled == value)
					return;

				_enabled = value;
				_owner.SyncSpecifiers();
				RaisePropertyChanged();
			}
		}

		/// <summary>
		/// Gets or sets the specifier pattern. Changes are persisted to the tool change immediately.
		/// </summary>
		public string Pattern
		{
			get => _pattern;
			set
			{
				if (_pattern == value)
					return;

				_pattern = value;
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
				if (value is null || _decision?.Value == value.Value)
					return;

				_decision = value;
				_owner.SyncSpecifiers();
				RaisePropertyChanged();
			}
		}

		/// <summary>
		/// Gets the command that removes this specifier rule from the tool.
		/// </summary>
		public ICommand RemoveCommand { get; }
	}
}
