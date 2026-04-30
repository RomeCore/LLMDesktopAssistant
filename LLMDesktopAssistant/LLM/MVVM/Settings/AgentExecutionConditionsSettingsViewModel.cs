using CommunityToolkit.Mvvm.ComponentModel;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Localization;

namespace LLMDesktopAssistant.LLM.MVVM.Settings
{
	public class CheckerTypeItem
	{
		public required string DisplayName { get; init; }
		public required AgentExecutionChecker Checker { get; init; }
	}

	[ViewModelFor(typeof(AgentExecutionConditionsSettingsView))]
	public class AgentExecutionConditionsSettingsViewModel : ViewModelBase
	{
		public AgentExecutionConditionsSettings ExecutionConditionsSettings { get; }

		public List<CheckerTypeItem> CheckerTypes { get; } =
		[
			new() {
				DisplayName = LocalizationManager.LocalizeStatic("execution_checker_always"),
				Checker = AgentExecutionChecker.Always
			},
		];

		private CheckerTypeItem? _selectedCheckerType;
		public CheckerTypeItem? SelectedCheckerType
		{
			get => _selectedCheckerType;
			set
			{
				if (SetProperty(ref _selectedCheckerType, value) && value != null)
				{
					ExecutionConditionsSettings.ExecutionChecker = value.Checker;
				}
			}
		}

		public AgentExecutionConditionsSettingsViewModel(AgentExecutionConditionsSettings settings)
		{
			ExecutionConditionsSettings = settings;

			// Try to match current checker
			if (settings.ExecutionChecker is AlwaysAgentExecutionChecker)
			{
				_selectedCheckerType = CheckerTypes[0];
			}
			else
			{
				_selectedCheckerType = CheckerTypes[0];
			}
		}
	}
}
