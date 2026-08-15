using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.Agents
{
	public class ReasoningLevelItem
	{
		public ReasoningSettings Value { get; init; }
		public string DisplayName { get; init; } = string.Empty;

		public override bool Equals(object? obj) => obj is ReasoningLevelItem other && Value == other.Value;
		public override int GetHashCode() => Value.GetHashCode();
	}

	/// <summary>
	/// ViewModel for the agent generation settings tab.
	/// All groups are resolved through their effective (inherited) scope, selected via
	/// the inheritance level combo boxes in the view.
	/// </summary>
	[ViewModelFor(typeof(AgentGenerationSettingsView))]
	public class AgentGenerationSettingsViewModel : ViewModelBase
	{
		private readonly ChatSettings _chatSettings;

		/// <summary>
		/// Gets the underlying generation settings.
		/// </summary>
		public AgentGenerationSettings GenerationSettings { get; }

		/// <summary>
		/// Gets the effective custom model group resolved by the current inheritance level.
		/// </summary>
		public CustomModelSettings EffectiveCustomModel => GenerationSettings.GetEffectiveCustomModel(_chatSettings);

		/// <summary>
		/// Gets the effective reasoning group resolved by the current inheritance level.
		/// </summary>
		public ReasoningOverrideSettings EffectiveReasoning => GenerationSettings.GetEffectiveReasoning(_chatSettings);

		/// <summary>
		/// Gets the effective temperature group resolved by the current inheritance level.
		/// </summary>
		public TemperatureSettings EffectiveTemperature => GenerationSettings.GetEffectiveTemperature(_chatSettings);

		/// <summary>
		/// Gets the effective max tokens group resolved by the current inheritance level.
		/// </summary>
		public MaxTokensSettings EffectiveMaxTokens => GenerationSettings.GetEffectiveMaxTokens(_chatSettings);

		/// <summary>
		/// Gets the effective additional parameters resolved by the current inheritance level.
		/// </summary>
		public RangeObservableCollection<AdditionalParameter> EffectiveAdditionalParameters => GenerationSettings.GetEffectiveAdditionalParameters(_chatSettings);

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
		/// <summary>
		/// Gets or sets the selected reasoning level of the effective reasoning group.
		/// </summary>
		public ReasoningLevelItem? SelectedReasoningLevel
		{
			get => _selectedReasoningLevel;
			set
			{
				if (SetProperty(ref _selectedReasoningLevel, value) && value != null)
				{
					EffectiveReasoning.ReasoningSettings = value.Value;
				}
			}
		}

		private InheritanceLevelItem _selectedCustomModelInheritance;
		/// <summary>
		/// Gets or sets the inheritance level for the custom model group.
		/// </summary>
		public InheritanceLevelItem SelectedCustomModelInheritance
		{
			get => _selectedCustomModelInheritance;
			set
			{
				if (SetProperty(ref _selectedCustomModelInheritance, value) && value != null)
					GenerationSettings.CustomModelInheritance = value.Value;
			}
		}

		private InheritanceLevelItem _selectedReasoningInheritance;
		/// <summary>
		/// Gets or sets the inheritance level for the reasoning group.
		/// </summary>
		public InheritanceLevelItem SelectedReasoningInheritance
		{
			get => _selectedReasoningInheritance;
			set
			{
				if (SetProperty(ref _selectedReasoningInheritance, value) && value != null)
					GenerationSettings.ReasoningInheritance = value.Value;
			}
		}

		private InheritanceLevelItem _selectedTemperatureInheritance;
		/// <summary>
		/// Gets or sets the inheritance level for the temperature group.
		/// </summary>
		public InheritanceLevelItem SelectedTemperatureInheritance
		{
			get => _selectedTemperatureInheritance;
			set
			{
				if (SetProperty(ref _selectedTemperatureInheritance, value) && value != null)
					GenerationSettings.TemperatureInheritance = value.Value;
			}
		}

		private InheritanceLevelItem _selectedMaxTokensInheritance;
		/// <summary>
		/// Gets or sets the inheritance level for the max tokens group.
		/// </summary>
		public InheritanceLevelItem SelectedMaxTokensInheritance
		{
			get => _selectedMaxTokensInheritance;
			set
			{
				if (SetProperty(ref _selectedMaxTokensInheritance, value) && value != null)
					GenerationSettings.MaxTokensInheritance = value.Value;
			}
		}

		private InheritanceLevelItem _selectedAdditionalParametersInheritance;
		/// <summary>
		/// Gets or sets the inheritance level for the additional parameters group.
		/// </summary>
		public InheritanceLevelItem SelectedAdditionalParametersInheritance
		{
			get => _selectedAdditionalParametersInheritance;
			set
			{
				if (SetProperty(ref _selectedAdditionalParametersInheritance, value) && value != null)
					GenerationSettings.AdditionalParametersInheritance = value.Value;
			}
		}

		/// <summary>
		/// Gets the command that adds a new additional parameter to the effective list.
		/// </summary>
		public ICommand AddParameterCommand { get; }

		/// <summary>
		/// Gets the command that removes an additional parameter from the effective list.
		/// </summary>
		public ICommand RemoveParameterCommand { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="AgentGenerationSettingsViewModel"/> class.
		/// </summary>
		/// <param name="settings">The generation settings to edit.</param>
		/// <param name="chatSettings">The chat settings used to resolve inherited settings.</param>
		public AgentGenerationSettingsViewModel(AgentGenerationSettings settings, ChatSettings chatSettings)
		{
			_chatSettings = chatSettings;
			GenerationSettings = settings;

			_selectedCustomModelInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == settings.CustomModelInheritance);
			_selectedReasoningInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == settings.ReasoningInheritance);
			_selectedTemperatureInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == settings.TemperatureInheritance);
			_selectedMaxTokensInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == settings.MaxTokensInheritance);
			_selectedAdditionalParametersInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == settings.AdditionalParametersInheritance);

			// Init selected reasoning item from the effective settings value
			_selectedReasoningLevel = ReasoningLevels.FirstOrDefault(r => r.Value == EffectiveReasoning.ReasoningSettings)
				?? ReasoningLevels[0];

			settings.PropertyChanged += GenerationSettings_PropertyChanged;

			AddParameterCommand = new RelayCommand(() =>
			{
				EffectiveAdditionalParameters.Add(new AdditionalParameter
				{
					Enabled = true,
					ParameterName = "new_parameter",
					ParameterValue = "\"value\""
				});
			});

			RemoveParameterCommand = new RelayCommand<AdditionalParameter?>(param =>
			{
				if (param != null)
					EffectiveAdditionalParameters.Remove(param);
			});
		}

		private void GenerationSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			// The generated inheritance level setters raise PropertyChanged with the
			// name of the inherited property (e.g. "CustomModel") when the level changes.
			switch (e.PropertyName)
			{
				case nameof(AgentGenerationSettings.CustomModel):
					_selectedCustomModelInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == GenerationSettings.CustomModelInheritance);
					RaisePropertyChanged(nameof(SelectedCustomModelInheritance));
					RaisePropertyChanged(nameof(EffectiveCustomModel));
					break;

				case nameof(AgentGenerationSettings.Reasoning):
					_selectedReasoningInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == GenerationSettings.ReasoningInheritance);
					RaisePropertyChanged(nameof(SelectedReasoningInheritance));
					RaisePropertyChanged(nameof(EffectiveReasoning));
					_selectedReasoningLevel = ReasoningLevels.FirstOrDefault(r => r.Value == EffectiveReasoning.ReasoningSettings)
						?? ReasoningLevels[0];
					RaisePropertyChanged(nameof(SelectedReasoningLevel));
					break;

				case nameof(AgentGenerationSettings.Temperature):
					_selectedTemperatureInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == GenerationSettings.TemperatureInheritance);
					RaisePropertyChanged(nameof(SelectedTemperatureInheritance));
					RaisePropertyChanged(nameof(EffectiveTemperature));
					break;

				case nameof(AgentGenerationSettings.MaxTokens):
					_selectedMaxTokensInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == GenerationSettings.MaxTokensInheritance);
					RaisePropertyChanged(nameof(SelectedMaxTokensInheritance));
					RaisePropertyChanged(nameof(EffectiveMaxTokens));
					break;

				case nameof(AgentGenerationSettings.AdditionalParameters):
					_selectedAdditionalParametersInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == GenerationSettings.AdditionalParametersInheritance);
					RaisePropertyChanged(nameof(SelectedAdditionalParametersInheritance));
					RaisePropertyChanged(nameof(EffectiveAdditionalParameters));
					break;
			}
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
				GenerationSettings.PropertyChanged -= GenerationSettings_PropertyChanged;
		}
	}
}
