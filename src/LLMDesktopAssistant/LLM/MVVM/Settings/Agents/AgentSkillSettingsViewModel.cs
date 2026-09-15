using System.ComponentModel;
using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Addons.Management;
using LLMDesktopAssistant.Addons.MVVM;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Prompting.Skills;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.Agents;

/// <summary>
/// ViewModel for the per-agent skill settings: the available skills rendered with the addon cards,
/// where every card edits the skill overrides of the effective skillset.
/// </summary>
[ViewModelFor(typeof(AgentSkillSettingsView))]
public class AgentSkillSettingsViewModel : AddonListViewModel<SkillInfo, SkillChange>
{
	private readonly ChatSettings _chatSettings;

	private InheritanceLevelItem _selectedSkillChangesInheritance;

	/// <summary>
	/// Gets the underlying agent skill settings.
	/// </summary>
	public AgentSkillSettings SkillSettings { get; }

	/// <summary>
	/// Gets the skillset resolved by the current inheritance level. The cards edit the changes of this set.
	/// </summary>
	public SkillsetSettings EffectiveSkillset => SkillSettings.GetEffectiveSkillset(_chatSettings);

	/// <summary>
	/// Gets or sets the inheritance level of the skillset.
	/// </summary>
	public InheritanceLevelItem SelectedSkillChangesInheritance
	{
		get => _selectedSkillChangesInheritance;
		set
		{
			if (SetProperty(ref _selectedSkillChangesInheritance, value) && value is not null)
				SkillSettings.SkillsetInheritance = value.Value;
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AgentSkillSettingsViewModel"/> class.
	/// </summary>
	/// <param name="settings">The agent skill settings.</param>
	/// <param name="chatSettings">The chat settings used to resolve the inherited skillset.</param>
	/// <param name="skillsetCollector">The collector that provides the available skills.</param>
	/// <param name="cardFactory">The factory that builds the skill cards.</param>
	/// <param name="addonInvalidator">The invalidator used to reload the addons before building the list.</param>
	public AgentSkillSettingsViewModel(AgentSkillSettings settings, ChatSettings chatSettings,
		IAddonSetCollector<SkillInfo> skillsetCollector,
		IAddonCardFactory<SkillInfo, SkillChange> cardFactory,
		IAddonManagerInvalidator addonInvalidator)
		: base(skillsetCollector, cardFactory, addonInvalidator)
	{
		SkillSettings = settings;
		_chatSettings = chatSettings;
		_selectedSkillChangesInheritance = InheritanceLevelItem.AllAgent.First(item => item.Value == settings.SkillsetInheritance);
		settings.PropertyChanged += SkillSettings_PropertyChanged;

		Update();
	}

	/// <inheritdoc/>
	protected override AddonCardContext<SkillInfo, SkillChange> CreateContext(SkillInfo addon) => new()
	{
		Addon = addon,
		SetConfig = EffectiveSkillset,
		TagClickCommand = TagClickCommand,
		OnDeleted = Update
	};

	private void SkillSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName != nameof(AgentSkillSettings.SkillsetInheritance))
			return;

		_selectedSkillChangesInheritance = InheritanceLevelItem.AllAgent.First(item => item.Value == SkillSettings.SkillsetInheritance);
		RaisePropertyChanged(nameof(SelectedSkillChangesInheritance));
		RaisePropertyChanged(nameof(EffectiveSkillset));
		Update();
	}

	/// <inheritdoc/>
	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);

		if (disposing)
			SkillSettings.PropertyChanged -= SkillSettings_PropertyChanged;
	}
}
