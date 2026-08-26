using System.ComponentModel;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Agents.Memory;
using LLMDesktopAssistant.Agents.SubAgents;
using LLMDesktopAssistant.LLM.Services.Prompting;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.Agents;

/// <summary>
/// ViewModel for the per-agent sub-agent settings: the list of available sub-agents with
/// per-agent overrides (enabled + model) built on reusable <see cref="SubAgentCardViewModel"/> cards.
/// </summary>
[ViewModelFor(typeof(AgentSubAgentSettingsView))]
public class AgentSubAgentSettingsViewModel : ViewModelBase
{
	private readonly ISubAgentSetBuildingService _subAgentSetBuilder;
	private readonly ISkillsetBuildingService _skillsetBuilder;
	private readonly ChatSettings _chatSettings;
	private ImmutableList<SubAgentCardViewModel> _allCards = [];

	/// <summary>
	/// Gets the underlying agent sub-agent settings.
	/// </summary>
	public AgentSubAgentSettings SubAgentSettings { get; }

	/// <summary>
	/// Gets the effective sub-agentset resolved by the current inheritance level.
	/// </summary>
	public SubAgentsetSettings EffectiveSubAgentset => SubAgentSettings.GetEffectiveSubAgentset(_chatSettings);

	/// <summary>
	/// Gets the effective sub-agent changes resolved by the current inheritance level.
	/// </summary>
	public RangeObservableCollection<SubAgentChange> EffectiveSubAgentChanges => EffectiveSubAgentset.SubAgentChanges;

	private InheritanceLevelItem _selectedSubAgentChangesInheritance;
	/// <summary>
	/// Gets or sets the inheritance level for the sub-agent changes group.
	/// </summary>
	public InheritanceLevelItem SelectedSubAgentChangesInheritance
	{
		get => _selectedSubAgentChangesInheritance;
		set
		{
			if (SetProperty(ref _selectedSubAgentChangesInheritance, value) && value != null)
				SubAgentSettings.SubAgentsetInheritance = value.Value;
		}
	}

	private string _searchText = string.Empty;
	/// <summary>
	/// Gets or sets the search text filtering the sub-agents by name, description and tags.
	/// </summary>
	public string SearchText
	{
		get => _searchText;
		set
		{
			if (SetProperty(ref _searchText, value))
				ApplyFilter();
		}
	}

	private readonly RangeObservableCollection<SubAgentCardViewModel> _subAgentItems = [];
	/// <summary>
	/// Gets or sets the filtered list of sub-agent cards with per-agent override settings.
	/// </summary>
	public ICollection<SubAgentCardViewModel> SubAgentItems
	{
		get => _subAgentItems;
		set => _subAgentItems.Reset(value);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AgentSubAgentSettingsViewModel"/> class.
	/// </summary>
	/// <param name="settings">The agent sub-agent settings.</param>
	/// <param name="chatSettings">The chat settings used to resolve inherited settings.</param>
	/// <param name="subAgentSetBuilder">The service providing the available sub-agents.</param>
	/// <param name="skillsetBuilder">The service providing the available skills for link checking.</param>
	public AgentSubAgentSettingsViewModel(AgentSubAgentSettings settings, ChatSettings chatSettings,
		ISubAgentSetBuildingService subAgentSetBuilder, ISkillsetBuildingService skillsetBuilder)
	{
		SubAgentSettings = settings;
		_chatSettings = chatSettings;
		_subAgentSetBuilder = subAgentSetBuilder;
		_skillsetBuilder = skillsetBuilder;

		_selectedSubAgentChangesInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == settings.SubAgentsetInheritance);
		settings.PropertyChanged += SubAgentSettings_PropertyChanged;

		UpdateSubAgents();
	}

	private void SubAgentSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName != nameof(AgentSubAgentSettings.SubAgentsetInheritance))
			return;

		_selectedSubAgentChangesInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == SubAgentSettings.SubAgentsetInheritance);
		RaisePropertyChanged(nameof(SelectedSubAgentChangesInheritance));
		RaisePropertyChanged(nameof(EffectiveSubAgentset));
		RaisePropertyChanged(nameof(EffectiveSubAgentChanges));
		UpdateSubAgents();
	}

	/// <summary>
	/// Refreshes the list of available sub-agents and rebuilds the cards
	/// with current per-agent overrides.
	/// </summary>
	public void UpdateSubAgents()
	{
		var subAgents = _subAgentSetBuilder.GetAvailableSubAgents().ToList();
		var subAgentNames = subAgents.Select(s => s.Name).ToHashSet();
		var skillNames = _skillsetBuilder.GetAvailableSkills().Select(s => s.Name).ToHashSet();
		var memoryBlockNames = SettingsManager.GetCategory<MemoryBlock>().GetAll().Select(kvp => kvp.Value.Name).ToHashSet();
		var changes = EffectiveSubAgentChanges.ToDictionary(c => c.SubAgentName, c => c);

		_allCards.ForEach(c => c.Dispose());
		_allCards = subAgents
			.Where(s => s.Diagnostic?.IsFatal != true)
			.Select(s =>
			{
				changes.TryGetValue(s.Name, out var existingChange);
				return new SubAgentCardViewModel(
					s,
					canToggle: true,
					change: existingChange,
					changes: EffectiveSubAgentChanges,
					settings: EffectiveSubAgentset,
					linkIssues: SubAgentLinkChecker.Check(s, skillNames, subAgentNames, memoryBlockNames),
					onTagClick: tag => SearchText = tag,
					onDeleted: UpdateSubAgents);
			})
			.ToImmutableList();

		ApplyFilter();
	}

	private void ApplyFilter()
	{
		var query = SearchText?.Trim() ?? string.Empty;
		IEnumerable<SubAgentCardViewModel> filtered = _allCards;
		if (query.Length > 0)
		{
			filtered = _allCards.Where(c =>
				c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
				c.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
				c.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase)));
		}

		SubAgentItems = filtered.ToImmutableList();
	}

	/// <inheritdoc/>
	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);

		if (disposing)
		{
			SubAgentSettings.PropertyChanged -= SubAgentSettings_PropertyChanged;
			_allCards.ForEach(c => c.Dispose());
		}
	}
}
