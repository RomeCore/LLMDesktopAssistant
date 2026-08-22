using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Controls.Dialogs;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Providers
{
	/// <summary>
	/// Represents a reasoning level option in the modifier editor dropdown.
	/// </summary>
	public class ModifierReasoningLevelItem
	{
		/// <summary>
		/// The reasoning settings value, or <see langword="null"/> for "not set".
		/// </summary>
		public ReasoningMode Value { get; init; }

		/// <summary>
		/// The display name of the reasoning level.
		/// </summary>
		public required string DisplayName { get; init; }
	}

	/// <summary>
	/// ViewModel for adding or editing a model modifier.
	/// </summary>
	[ViewModelFor(typeof(ConfigureModifierDialogView))]
	public class ConfigureModifierDialogViewModel : ViewModelBase
	{
		private readonly ModelModifiersConfiguration _configuration;

		/// <summary>
		/// Gets the modifier being edited.
		/// </summary>
		public ModelModifier EditingModifier { get; }

		/// <summary>
		/// Gets a value indicating whether the dialog is in edit mode.
		/// </summary>
		public bool IsEditMode { get; }

		/// <summary>
		/// Gets the title text of the dialog.
		/// </summary>
		public string TitleText => IsEditMode
			? LocalizationManager.LocalizeStatic("model.modifier.edit.title")
			: LocalizationManager.LocalizeStatic("model.modifier.add.title");

		private string? _errorMessage;
		/// <summary>
		/// Gets or sets the validation error message.
		/// </summary>
		public string? ErrorMessage
		{
			get => _errorMessage;
			set => SetProperty(ref _errorMessage, value);
		}

		private ModifierReasoningLevelItem _selectedReasoning;
		/// <summary>
		/// Gets or sets the selected reasoning level.
		/// </summary>
		public ModifierReasoningLevelItem SelectedReasoning
		{
			get => _selectedReasoning;
			set
			{
				if (SetProperty(ref _selectedReasoning, value))
					EditingModifier.ReasoningMode = value.Value;
			}
		}

		/// <summary>
		/// Gets the list of reasoning level options.
		/// </summary>
		public List<ModifierReasoningLevelItem> ReasoningLevels { get; } =
		[
			new() { Value = ReasoningMode.Default, DisplayName = LocalizationManager.LocalizeStatic("model.modifier.reasoning.default") },
			new() { Value = ReasoningMode.Disabled, DisplayName = LocalizationManager.LocalizeStatic("model.modifier.reasoning.disabled") },
			new() { Value = ReasoningMode.None, DisplayName = LocalizationManager.LocalizeStatic("model.modifier.reasoning.none") },
			new() { Value = ReasoningMode.Minimal, DisplayName = LocalizationManager.LocalizeStatic("model.modifier.reasoning.minimal") },
			new() { Value = ReasoningMode.Low, DisplayName = LocalizationManager.LocalizeStatic("model.modifier.reasoning.low") },
			new() { Value = ReasoningMode.Medium, DisplayName = LocalizationManager.LocalizeStatic("model.modifier.reasoning.medium") },
			new() { Value = ReasoningMode.High, DisplayName = LocalizationManager.LocalizeStatic("model.modifier.reasoning.high") },
			new() { Value = ReasoningMode.XHigh, DisplayName = LocalizationManager.LocalizeStatic("model.modifier.reasoning.xhigh") },
			new() { Value = ReasoningMode.Maximum, DisplayName = LocalizationManager.LocalizeStatic("model.modifier.reasoning.maximum") },
		];

		/// <summary>
		/// Gets the command that saves the modifier.
		/// </summary>
		public ICommand CloseCommand { get; }

		/// <summary>
		/// Gets the command that adds a new additional parameter.
		/// </summary>
		public ICommand AddParameterCommand { get; }

		/// <summary>
		/// Gets the command that removes an additional parameter.
		/// </summary>
		public ICommand RemoveParameterCommand { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ConfigureModifierDialogViewModel"/> class.
		/// </summary>
		/// <param name="modifier">The modifier to edit.</param>
		/// <param name="isEditMode">Whether the modifier already exists in the configuration.</param>
		public ConfigureModifierDialogViewModel(ModelModifier modifier, bool isEditMode)
		{
			_configuration = SettingsManager.Get<ModelModifiersConfiguration>();
			EditingModifier = modifier;
			IsEditMode = isEditMode;

			_selectedReasoning = ReasoningLevels.FirstOrDefault(r => r.Value == modifier.ReasoningMode) ?? ReasoningLevels[0];

			CloseCommand = new RelayCommand(Close);
			AddParameterCommand = new RelayCommand(() =>
			{
				EditingModifier.AdditionalParameters.Add(new AdditionalGenerationParameter
				{
					Enabled = true,
					ParameterName = "new_parameter",
					ParameterValue = "\"value\""
				});
			});
			RemoveParameterCommand = new RelayCommand<AdditionalGenerationParameter?>(param =>
			{
				if (param != null)
					EditingModifier.AdditionalParameters.Remove(param);
			});
		}

		private void Close()
		{
			ErrorMessage = null;

			if (string.IsNullOrWhiteSpace(EditingModifier.Name))
			{
				ErrorMessage = LocalizationManager.LocalizeStatic("model.modifier.name.required.error");
				return;
			}

			if (EditingModifier.Name.Contains('$'))
			{
				ErrorMessage = LocalizationManager.LocalizeStatic("model.modifier.name.invalid.error");
				return;
			}

			if (_configuration.Modifiers.Any(m => !ReferenceEquals(m, EditingModifier) && m.Name == EditingModifier.Name))
			{
				ErrorMessage = LocalizationManager.LocalizeStatic("model.modifier.name.duplicate.error");
				return;
			}

			DialogManager.CloseDialog(true);
		}
	}
}
