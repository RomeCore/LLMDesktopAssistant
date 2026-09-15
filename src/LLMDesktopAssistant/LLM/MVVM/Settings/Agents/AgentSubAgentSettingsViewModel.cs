using System.ComponentModel;
using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Addons.Management;
using LLMDesktopAssistant.Addons.MVVM;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Agents.SubAgents;
using LLMDesktopAssistant.LLM.Settings;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.Agents;

/// <summary>
/// ViewModel for the per-agent sub-agent settings: the available sub-agents rendered with the addon cards,
/// where every card edits the sub-agent overrides of the effective sub-agentset.
/// </summary>
[ViewModelFor(typeof(AgentSubAgentSettingsView))]
public class AgentSubAgentSettingsViewModel : AddonListViewModel<SubAgentInfo, SubAgentChange>
{
	private readonly ChatSettings _chatSettings;

	private InheritanceLevelItem _selectedSubAgentChangesInheritance;

	/// <summary>
	/// Gets the underlying agent sub-agent settings.
	/// </summary>
	public AgentSubAgentSettings SubAgentSettings { get; }

	/// <summary>
	/// Gets the sub-agentset resolved by the current inheritance level. The cards edit the changes of this set.
	/// </summary>
	public SubAgentsetSettings EffectiveSubAgentset => SubAgentSettings.GetEffectiveSubAgentset(_chatSettings);

	/// <summary>
	/// Gets or sets the inheritance level of the sub-agentset.
	/// </summary>
	public InheritanceLevelItem SelectedSubAgentChangesInheritance
	{
		get => _selectedSubAgentChangesInheritance;
		set
		{
			if (SetProperty(ref _selectedSubAgentChangesInheritance, value) && value is not null)
				SubAgentSettings.SubAgentsetInheritance = value.Value;
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AgentSubAgentSettingsViewModel"/> class.
	/// </summary>
	/// <param name="settings">The agent sub-agent settings.</param>
	/// <param name="chatSettings">The chat settings used to resolve the inherited sub-agentset.</param>
	/// <param name="subAgentsetCollector">The collector that provides the available sub-agents.</param>
	/// <param name="cardFactory">The factory that builds the sub-agent cards.</param>
	/// <param name="addonInvalidator">The invalidator used to reload the addons before building the list.</param>
	public AgentSubAgentSettingsViewModel(AgentSubAgentSettings settings, ChatSettings chatSettings,
		IAddonSetCollector<SubAgentInfo> subAgentsetCollector,
		IAddonCardFactory<SubAgentInfo, SubAgentChange> cardFactory,
		IAddonManagerInvalidator addonInvalidator)
		: base(subAgentsetCollector, cardFactory, addonInvalidator)
	{
		SubAgentSettings = settings;
		_chatSettings = chatSettings;
		_selectedSubAgentChangesInheritance = InheritanceLevelItem.AllAgent.First(item => item.Value == settings.SubAgentsetInheritance);
		settings.PropertyChanged += SubAgentSettings_PropertyChanged;

		Update();
	}

	/// <inheritdoc/>
	protected override AddonCardContext<SubAgentInfo, SubAgentChange> CreateContext(SubAgentInfo addon) => new()
	{
		Addon = addon,
		SetConfig = EffectiveSubAgentset,
		TagClickCommand = TagClickCommand,
		OnDeleted = Update
	};

	private void SubAgentSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName != nameof(AgentSubAgentSettings.SubAgentsetInheritance))
			return;

		_selectedSubAgentChangesInheritance = InheritanceLevelItem.AllAgent.First(item => item.Value == SubAgentSettings.SubAgentsetInheritance);
		RaisePropertyChanged(nameof(SelectedSubAgentChangesInheritance));
		RaisePropertyChanged(nameof(EffectiveSubAgentset));
		Update();
	}

	/// <inheritdoc/>
	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);

		if (disposing)
			SubAgentSettings.PropertyChanged -= SubAgentSettings_PropertyChanged;
	}
}
