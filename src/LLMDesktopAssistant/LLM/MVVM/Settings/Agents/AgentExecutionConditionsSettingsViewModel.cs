using System.ComponentModel;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Settings;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.Agents
{
	/// <summary>
	/// ViewModel for the agent execution conditions settings tab.
	/// The mention group is resolved through its effective (inherited) scope, selected via
	/// the inheritance level combo box in the view.
	/// </summary>
	[ViewModelFor(typeof(AgentExecutionConditionsSettingsView))]
	public class AgentExecutionConditionsSettingsViewModel : ViewModelBase
	{
		private readonly ChatSettings _chatSettings;

		/// <summary>
		/// Gets the underlying execution conditions settings.
		/// </summary>
		public AgentExecutionConditionsSettings ExecutionConditionsSettings { get; }

		/// <summary>
		/// Gets the effective mention group resolved by the current inheritance level.
		/// </summary>
		public MentionSettings EffectiveMention => ExecutionConditionsSettings.GetEffectiveMention(_chatSettings);

		private InheritanceLevelItem _selectedMentionInheritance;
		/// <summary>
		/// Gets or sets the inheritance level for the mention group.
		/// </summary>
		public InheritanceLevelItem SelectedMentionInheritance
		{
			get => _selectedMentionInheritance;
			set
			{
				if (SetProperty(ref _selectedMentionInheritance, value) && value != null)
					ExecutionConditionsSettings.MentionInheritance = value.Value;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AgentExecutionConditionsSettingsViewModel"/> class.
		/// </summary>
		/// <param name="settings">The execution conditions settings to edit.</param>
		/// <param name="chatSettings">The chat settings used to resolve inherited settings.</param>
		public AgentExecutionConditionsSettingsViewModel(AgentExecutionConditionsSettings settings, ChatSettings chatSettings)
		{
			_chatSettings = chatSettings;
			ExecutionConditionsSettings = settings;

			_selectedMentionInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == settings.MentionInheritance);
			settings.PropertyChanged += ExecutionConditionsSettings_PropertyChanged;
		}

		private void ExecutionConditionsSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			// The generated inheritance level setter raises PropertyChanged with the
			// name of the inherited property ("Mention") when the level changes.
			if (e.PropertyName != "Mention")
				return;

			_selectedMentionInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == ExecutionConditionsSettings.MentionInheritance);
			RaisePropertyChanged(nameof(SelectedMentionInheritance));
			RaisePropertyChanged(nameof(EffectiveMention));
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
				ExecutionConditionsSettings.PropertyChanged -= ExecutionConditionsSettings_PropertyChanged;
		}
	}
}
