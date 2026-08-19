using System.ComponentModel;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Services.Prompting;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Prompting.Skills;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.Agents;

/// <summary>
/// ViewModel for the per-agent skill settings: the list of available skills with
/// per-agent overrides (enabled + injection mode) built on reusable <see cref="SkillCardViewModel"/> cards.
/// </summary>
[ViewModelFor(typeof(AgentSkillSettingsView))]
public class AgentSkillSettingsViewModel : ViewModelBase
{
	private readonly ISkillsetBuildingService _skillsetBuilder;
	private readonly ChatSettings _chatSettings;
	private ImmutableList<SkillCardViewModel> _allCards = [];

	/// <summary>
	/// Gets the underlying agent skill settings.
	/// </summary>
	public AgentSkillSettings SkillSettings { get; }

	/// <summary>
	/// Gets the effective skill changes resolved by the current inheritance level.
	/// </summary>
	public RangeObservableCollection<SkillChange> EffectiveSkillChanges => SkillSettings.GetEffectiveSkillChanges(_chatSettings);

	private InheritanceLevelItem _selectedSkillChangesInheritance;
	/// <summary>
	/// Gets or sets the inheritance level for the skill changes group.
	/// </summary>
	public InheritanceLevelItem SelectedSkillChangesInheritance
	{
		get => _selectedSkillChangesInheritance;
		set
		{
			if (SetProperty(ref _selectedSkillChangesInheritance, value) && value != null)
				SkillSettings.SkillChangesInheritance = value.Value;
		}
	}

	private string _searchText = string.Empty;
	/// <summary>
	/// Gets or sets the search text filtering the skills by name, description and tags.
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

	private RangeObservableCollection<SkillCardViewModel> _skillItems = [];
	/// <summary>
	/// Gets or sets the filtered list of skill cards with per-agent override settings.
	/// </summary>
	public ICollection<SkillCardViewModel> SkillItems
	{
		get => _skillItems;
		set
		{
			_skillItems.Reset(value);
			RaisePropertyChanged(nameof(SkillItems));
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AgentSkillSettingsViewModel"/> class.
	/// </summary>
	/// <param name="settings">The agent skill settings.</param>
	/// <param name="skillsetBuilder">The service providing the available skills.</param>
	/// <param name="chatSettings">The chat settings used to resolve inherited settings.</param>
	public AgentSkillSettingsViewModel(AgentSkillSettings settings,
		ISkillsetBuildingService skillsetBuilder, ChatSettings chatSettings)
	{
		SkillSettings = settings;
		_skillsetBuilder = skillsetBuilder;
		_chatSettings = chatSettings;

		_selectedSkillChangesInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == settings.SkillChangesInheritance);
		settings.PropertyChanged += SkillSettings_PropertyChanged;

		UpdateSkills();
	}

	private void SkillSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName != nameof(AgentSkillSettings.SkillChangesInheritance))
			return;

		_selectedSkillChangesInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == SkillSettings.SkillChangesInheritance);
		RaisePropertyChanged(nameof(SelectedSkillChangesInheritance));
		RaisePropertyChanged(nameof(EffectiveSkillChanges));
		UpdateSkills();
	}

	/// <summary>
	/// Refreshes the list of available skills and rebuilds the cards
	/// with current per-agent overrides.
	/// </summary>
	public void UpdateSkills()
	{
		var allSkills = _skillsetBuilder.GetAvailableSkills();
		var changes = EffectiveSkillChanges.ToDictionary(c => c.SkillName, c => c);

		_allCards = allSkills
			.Where(s => s.Diagnostic?.IsFatal != true)
			.Select(s =>
			{
				changes.TryGetValue(s.Name, out var existingChange);
				return new SkillCardViewModel(
					s,
					canToggle: true,
					change: existingChange,
					changes: EffectiveSkillChanges,
					onTagClick: tag => SearchText = tag,
					onDeleted: UpdateSkills);
			})
			.ToImmutableList();

		ApplyFilter();
	}

	private void ApplyFilter()
	{
		var query = SearchText?.Trim() ?? string.Empty;
		IEnumerable<SkillCardViewModel> filtered = _allCards;
		if (query.Length > 0)
		{
			filtered = _allCards.Where(c =>
				c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
				c.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
				c.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase)));
		}

		SkillItems = filtered.ToImmutableList();
	}

	/// <inheritdoc/>
	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);

		if (disposing)
			SkillSettings.PropertyChanged -= SkillSettings_PropertyChanged;
	}
}
