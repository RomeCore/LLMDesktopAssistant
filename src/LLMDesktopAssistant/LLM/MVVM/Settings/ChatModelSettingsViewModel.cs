using System.ComponentModel;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// ViewModel for the Models settings tab.
	/// All model selectors are resolved through the effective (inherited) scope, selected via
	/// the inheritance level combo box in the view.
	/// </summary>
	[ViewModelFor(typeof(ChatModelSettingsView))]
	public class ChatModelSettingsViewModel : ViewModelBase
	{
		/// <summary>
		/// Gets the underlying model settings.
		/// </summary>
		public ChatModelSettings ModelSettings { get; }

		/// <summary>
		/// Gets the effective model selection resolved by the current inheritance level.
		/// </summary>
		public ModelSelectionSettings EffectiveSelection => ModelSettings.GetEffectiveSelection();

		private InheritanceLevelItem _selectedSelectionInheritance;
		/// <summary>
		/// Gets or sets the inheritance level for the model selection group.
		/// </summary>
		public InheritanceLevelItem SelectedSelectionInheritance
		{
			get => _selectedSelectionInheritance;
			set
			{
				if (SetProperty(ref _selectedSelectionInheritance, value) && value != null)
					ModelSettings.SelectionInheritance = value.Value;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ChatModelSettingsViewModel"/> class.
		/// </summary>
		/// <param name="settings">The model settings to edit.</param>
		public ChatModelSettingsViewModel(ChatModelSettings settings)
		{
			ModelSettings = settings;

			_selectedSelectionInheritance = InheritanceLevelItem.AllProfile.First(i => i.Value == settings.SelectionInheritance);
			settings.PropertyChanged += ModelSettings_PropertyChanged;
		}

		private void ModelSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != nameof(ChatModelSettings.SelectionInheritance))
				return;

			_selectedSelectionInheritance = InheritanceLevelItem.AllProfile.First(i => i.Value == ModelSettings.SelectionInheritance);
			RaisePropertyChanged(nameof(SelectedSelectionInheritance));
			RaisePropertyChanged(nameof(EffectiveSelection));
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
				ModelSettings.PropertyChanged -= ModelSettings_PropertyChanged;
		}
	}
}
