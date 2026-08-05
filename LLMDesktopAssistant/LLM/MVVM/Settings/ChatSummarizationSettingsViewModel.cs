using System.ComponentModel;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// ViewModel for the Auto-summarization settings tab.
	/// All options are resolved through the effective (inherited) scope, selected via
	/// the inheritance level combo box in the view.
	/// </summary>
	[ViewModelFor(typeof(ChatSummarizationSettingsView))]
	public class ChatSummarizationSettingsViewModel : ViewModelBase
	{
		/// <summary>
		/// Gets the underlying summarization settings.
		/// </summary>
		public ChatSummarizationSettings SummarizationSettings { get; }

		/// <summary>
		/// Gets the effective auto-summarization options resolved by the current inheritance level.
		/// </summary>
		public SummarizationOptionsSettings EffectiveOptions => SummarizationSettings.GetEffectiveOptions();

		private InheritanceLevelItem _selectedOptionsInheritance;
		/// <summary>
		/// Gets or sets the inheritance level for the auto-summarization options group.
		/// </summary>
		public InheritanceLevelItem SelectedOptionsInheritance
		{
			get => _selectedOptionsInheritance;
			set
			{
				if (SetProperty(ref _selectedOptionsInheritance, value) && value != null)
					SummarizationSettings.OptionsInheritance = value.Value;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ChatSummarizationSettingsViewModel"/> class.
		/// </summary>
		/// <param name="settings">The summarization settings to edit.</param>
		public ChatSummarizationSettingsViewModel(ChatSummarizationSettings settings)
		{
			SummarizationSettings = settings;

			_selectedOptionsInheritance = InheritanceLevelItem.AllProfile.First(i => i.Value == settings.OptionsInheritance);
			settings.PropertyChanged += SummarizationSettings_PropertyChanged;
		}

		private void SummarizationSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != nameof(ChatSummarizationSettings.OptionsInheritance))
				return;

			_selectedOptionsInheritance = InheritanceLevelItem.AllProfile.First(i => i.Value == SummarizationSettings.OptionsInheritance);
			RaisePropertyChanged(nameof(SelectedOptionsInheritance));
			RaisePropertyChanged(nameof(EffectiveOptions));
		}
	}
}
