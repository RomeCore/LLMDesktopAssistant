using System.ComponentModel;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// ViewModel for the Memory settings tab.
	/// All options are resolved through the effective (inherited) scope, selected via
	/// the inheritance level combo boxes in the view.
	/// </summary>
	[ViewModelFor(typeof(ChatMemorySettingsView))]
	public class ChatMemorySettingsViewModel : ViewModelBase
	{
		/// <summary>
		/// Gets the underlying chat memory settings.
		/// </summary>
		public ChatMemorySettings MemorySettings { get; }

		/// <summary>
		/// Gets the effective memory options resolved by the current inheritance level.
		/// </summary>
		public MemorySettings EffectiveMemoryOptions => MemorySettings.GetEffectiveMemoryOptions();

		/// <summary>
		/// Gets the effective auto-summarization options resolved by the current inheritance level.
		/// </summary>
		public SummarizationOptionsSettings EffectiveSummarization => MemorySettings.GetEffectiveSummarization();

		private InheritanceLevelItem _selectedMemoryOptionsInheritance;
		/// <summary>
		/// Gets or sets the inheritance level for the memory options group.
		/// </summary>
		public InheritanceLevelItem SelectedMemoryOptionsInheritance
		{
			get => _selectedMemoryOptionsInheritance;
			set
			{
				if (SetProperty(ref _selectedMemoryOptionsInheritance, value) && value != null)
					MemorySettings.MemoryOptionsInheritance = value.Value;
			}
		}

		private InheritanceLevelItem _selectedSummarizationInheritance;
		/// <summary>
		/// Gets or sets the inheritance level for the auto-summarization options group.
		/// </summary>
		public InheritanceLevelItem SelectedSummarizationInheritance
		{
			get => _selectedSummarizationInheritance;
			set
			{
				if (SetProperty(ref _selectedSummarizationInheritance, value) && value != null)
					MemorySettings.SummarizationInheritance = value.Value;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ChatMemorySettingsViewModel"/> class.
		/// </summary>
		/// <param name="settings">The chat memory settings to edit.</param>
		public ChatMemorySettingsViewModel(ChatMemorySettings settings)
		{
			MemorySettings = settings;

			_selectedMemoryOptionsInheritance = InheritanceLevelItem.AllProfile.First(i => i.Value == settings.MemoryOptionsInheritance);
			_selectedSummarizationInheritance = InheritanceLevelItem.AllProfile.First(i => i.Value == settings.SummarizationInheritance);
			settings.PropertyChanged += MemorySettings_PropertyChanged;
		}

		private void MemorySettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(ChatMemorySettings.MemoryOptionsInheritance):
					_selectedMemoryOptionsInheritance = InheritanceLevelItem.AllProfile.First(i => i.Value == MemorySettings.MemoryOptionsInheritance);
					RaisePropertyChanged(nameof(SelectedMemoryOptionsInheritance));
					RaisePropertyChanged(nameof(EffectiveMemoryOptions));
					break;

				case nameof(ChatMemorySettings.SummarizationInheritance):
					_selectedSummarizationInheritance = InheritanceLevelItem.AllProfile.First(i => i.Value == MemorySettings.SummarizationInheritance);
					RaisePropertyChanged(nameof(SelectedSummarizationInheritance));
					RaisePropertyChanged(nameof(EffectiveSummarization));
					break;
			}
		}
	}
}
