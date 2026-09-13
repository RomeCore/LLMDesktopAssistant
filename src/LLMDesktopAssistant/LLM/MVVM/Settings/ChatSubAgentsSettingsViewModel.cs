using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Addons.Management;
using LLMDesktopAssistant.Agents.Memory;
using LLMDesktopAssistant.Agents.SubAgents;
using LLMDesktopAssistant.Controls.Dialogs;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Prompting.Skills;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.MVVM.Settings;

/// <summary>
/// ViewModel for the chat-level sub-agent settings: the list of available sub-agents with
/// search, creation and file actions. The sub-agent addon sources (packs, directories, files)
/// are configured on the shared addons settings page.
/// </summary>
[ViewModelFor(typeof(ChatSubAgentsSettingsView))]
public class ChatSubAgentsSettingsViewModel : ViewModelBase
{
	private readonly IAddonSetCollector<SubAgentInfo> _subAgentsetCollector;
	private readonly IAddonSetCollector<SkillInfo> _skillsetBuilder;
	private readonly IAddonManagerInvalidator _addonsInvalidator;
	private ImmutableList<SubAgentCardViewModel> _allCards = [];

	/// <summary>
	/// Gets the underlying chat sub-agent settings.
	/// </summary>
	public ChatSubAgentSettings SubAgentSettings { get; }

	private string _searchText = string.Empty;
	/// <summary>
	/// Gets or sets the search text filtering the available sub-agents by name, description and tags.
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

	private RangeObservableCollection<SubAgentCardViewModel> _availableSubAgents = [];
	/// <summary>
	/// Gets or sets the filtered list of available sub-agents.
	/// </summary>
	public ICollection<SubAgentCardViewModel> AvailableSubAgents
	{
		get => _availableSubAgents;
		set => _availableSubAgents.Reset(value);
	}

	/// <summary>
	/// Gets the command that refreshes the list of available sub-agents from disk.
	/// </summary>
	public ICommand RefreshSubAgentsCommand { get; }

	/// <summary>
	/// Gets the command that creates a new sub-agent file from a template.
	/// </summary>
	public ICommand CreateSubAgentCommand { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ChatSubAgentsSettingsViewModel"/> class.
	/// </summary>
	/// <param name="settings">The chat sub-agent settings.</param>
	/// <param name="subAgentsetCollector">The service providing the available sub-agents.</param>
	/// <param name="skillsetBuilder">The service providing the available skills for link checking.</param>
	/// <param name="addonsInvalidator">The invalidator used to reload addons before building the list.</param>
	public ChatSubAgentsSettingsViewModel(ChatSubAgentSettings settings,
		IAddonSetCollector<SubAgentInfo> subAgentsetCollector, IAddonSetCollector<SkillInfo> skillsetBuilder,
		IAddonManagerInvalidator addonsInvalidator)
	{
		SubAgentSettings = settings;
		_subAgentsetCollector = subAgentsetCollector;
		_skillsetBuilder = skillsetBuilder;
		_addonsInvalidator = addonsInvalidator;

		RefreshSubAgentsCommand = new RelayCommand(UpdateSubAgents);
		CreateSubAgentCommand = new AsyncRelayCommand(CreateSubAgentAsync);

		UpdateSubAgents();
	}

	/// <summary>
	/// Refreshes the list of available sub-agents from the sub-agent addon set.
	/// </summary>
	public void UpdateSubAgents()
	{
		_addonsInvalidator.Reload();

		var subAgents = _subAgentsetCollector.GetAvailableAddons().ToList();
		var subAgentNames = subAgents.Select(s => s.Name).ToHashSet();
		var skillNames = _skillsetBuilder.GetAvailableAddons().Select(s => s.Name).ToHashSet();
		var memoryBlockNames = SettingsManager.GetCategory<MemoryBlock>().GetAll().Select(kvp => kvp.Value.Name).ToHashSet();

		_allCards.ForEach(c => c.Dispose());
		_allCards = subAgents
			.Select(s => new SubAgentCardViewModel(
				s,
				canToggle: false,
				linkIssues: SubAgentLinkChecker.Check(s, skillNames, subAgentNames, memoryBlockNames),
				onTagClick: tag => SearchText = tag,
				onDeleted: UpdateSubAgents))
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

		AvailableSubAgents = filtered.ToImmutableList();
	}

	private async Task CreateSubAgentAsync()
	{
		var dialog = new TextInputDialogViewModel
		{
			Title = LocalizationManager.LocalizeStatic("settings.sub_agents.create.title"),
			Description = LocalizationManager.LocalizeStatic("settings.sub_agents.create.description"),
			Label = LocalizationManager.LocalizeStatic("settings.sub_agents.create.name.label"),
			Placeholder = LocalizationManager.LocalizeStatic("settings.sub_agents.create.name.placeholder"),
			SubmitText = LocalizationManager.LocalizeStatic("common.create"),
			CancelText = LocalizationManager.LocalizeStatic("common.cancel"),
			IsRequired = true
		};

		var name = (string?)await DialogManager.ShowDialogAsync(dialog);
		if (string.IsNullOrEmpty(name))
			return;

		var toast = ServiceRegistry.Provider.GetRequiredService<IToastService>();
		if (!SubAgentName.IsValidSubAgentName(name))
		{
			toast.ShowError(LocalizationManager.LocalizeStatic("settings.sub_agents.create.title"),
				LocalizationManager.LocalizeStatic("settings.sub_agents.create.error.invalid_name"));
			return;
		}

		var path = Path.Combine(Directories.Agents, $"{name}.md");
		if (File.Exists(path))
		{
			toast.ShowError(LocalizationManager.LocalizeStatic("settings.sub_agents.create.title"),
				LocalizationManager.LocalizeStatic("settings.sub_agents.create.error.exists"));
			return;
		}

		try
		{
			Directory.CreateDirectory(Directories.Agents);
			File.WriteAllText(path, BuildTemplate(name));
			UpdateSubAgents();

			toast.ShowSuccess(LocalizationManager.LocalizeStatic("settings.sub_agents.create.success"));
			Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
		}
		catch (Exception ex)
		{
			toast.ShowError(LocalizationManager.LocalizeStatic("common.error"), ex.Message);
		}
	}

	private static string BuildTemplate(string name) => $"""
		---
		name: {name}
		description: A sub-agent that helps with specific tasks.
		---

		# {name}

		Write the instructions for this sub-agent here.
		""";

	/// <inheritdoc/>
	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);

		if (disposing)
		{
			_allCards.ForEach(c => c.Dispose());
		}
	}
}
