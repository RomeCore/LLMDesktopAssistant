using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Localization;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.Agents
{
	public class ReasoningLevelItem
	{
		public ReasoningSettings Value { get; init; }
		public string DisplayName { get; init; } = string.Empty;

		public override bool Equals(object? obj) => obj is ReasoningLevelItem other && Value == other.Value;
		public override int GetHashCode() => Value.GetHashCode();
	}

	[ViewModelFor(typeof(AgentGenerationSettingsView))]
	public class AgentGenerationSettingsViewModel : ViewModelBase
	{
		public AgentGenerationSettings GenerationSettings { get; }

		public List<ReasoningLevelItem> ReasoningLevels { get; } =
		[
			new() { Value = ReasoningSettings.Default,   DisplayName = LocalizationManager.LocalizeStatic("reasoning_default") },
			new() { Value = ReasoningSettings.Disabled,  DisplayName = LocalizationManager.LocalizeStatic("reasoning_disabled") },
			new() { Value = ReasoningSettings.None,      DisplayName = LocalizationManager.LocalizeStatic("reasoning_none") },
			new() { Value = ReasoningSettings.Minimal,   DisplayName = LocalizationManager.LocalizeStatic("reasoning_minimal") },
			new() { Value = ReasoningSettings.Low,       DisplayName = LocalizationManager.LocalizeStatic("reasoning_low") },
			new() { Value = ReasoningSettings.Medium,    DisplayName = LocalizationManager.LocalizeStatic("reasoning_medium") },
			new() { Value = ReasoningSettings.High,      DisplayName = LocalizationManager.LocalizeStatic("reasoning_high") },
			new() { Value = ReasoningSettings.XHigh,     DisplayName = LocalizationManager.LocalizeStatic("reasoning_xhigh") },
			new() { Value = ReasoningSettings.Maximum,   DisplayName = LocalizationManager.LocalizeStatic("reasoning_maximum") },
		];

		private ReasoningLevelItem? _selectedReasoningLevel;
		public ReasoningLevelItem? SelectedReasoningLevel
		{
			get => _selectedReasoningLevel;
			set
			{
				if (SetProperty(ref _selectedReasoningLevel, value) && value != null)
				{
					GenerationSettings.ReasoningSettings = value.Value;
				}
			}
		}

		public ICommand AddParameterCommand { get; }
		public ICommand RemoveParameterCommand { get; }

		public AgentGenerationSettingsViewModel(AgentGenerationSettings settings)
		{
			GenerationSettings = settings;

			// Init selected reasoning item from current settings value
			_selectedReasoningLevel = ReasoningLevels.FirstOrDefault(r => r.Value == settings.ReasoningSettings)
				?? ReasoningLevels[0];

			AddParameterCommand = new RelayCommand(() =>
			{
				GenerationSettings.AdditionalParameters.Add(new AdditionalParameter
				{
					Enabled = true,
					ParameterName = "new_parameter",
					ParameterValue = "\"value\""
				});
			});

			RemoveParameterCommand = new RelayCommand<AdditionalParameter?>(param =>
			{
				if (param != null)
					GenerationSettings.AdditionalParameters.Remove(param);
			});
		}
	}
}
