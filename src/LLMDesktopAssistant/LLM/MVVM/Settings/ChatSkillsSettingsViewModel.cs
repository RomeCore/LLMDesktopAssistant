using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Addons.Management;
using LLMDesktopAssistant.Controls.Dialogs;
using LLMDesktopAssistant.LLM.Services.Prompting;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Prompting.Skills;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.MVVM.Settings;

/// <summary>
/// ViewModel for the chat-level skill settings: the list of available skills with
/// search, creation and file actions, plus the skill sources group.
/// </summary>
[ViewModelFor(typeof(ChatSkillsSettingsView))]
public class ChatSkillsSettingsViewModel : ViewModelBase
{
	private readonly IAddonSetCollector<SkillInfo> _skillsetBuilder;
	private readonly IAddonManagerInvalidator _addonInvalidator;
	private ImmutableList<SkillCardViewModel> _allCards = [];

	/// <summary>
	/// Gets the underlying chat skill settings.
	/// </summary>
	public ChatSkillSettings SkillSettings { get; }

	
	private string _searchText = string.Empty;
	/// <summary>
	/// Gets or sets the search text filtering the available skills by name, description and tags.
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

	private RangeObservableCollection<SkillCardViewModel> _availableSkills = [];
	/// <summary>
	/// Gets or sets the filtered list of available skills.
	/// </summary>
	public ICollection<SkillCardViewModel> AvailableSkills
	{
		get => _availableSkills;
		set => _availableSkills.Reset(value);
	}

	/// <summary>
	/// Gets the command that refreshes the list of available skills from disk.
	/// </summary>
	public ICommand RefreshSkillsCommand { get; }

	/// <summary>
	/// Gets the command that creates a new skill file from a template.
	/// </summary>
	public ICommand CreateSkillCommand { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ChatSkillsSettingsViewModel"/> class.
	/// </summary>
	public ChatSkillsSettingsViewModel(ChatSkillSettings settings, IAddonSetCollector<SkillInfo> skillsetBuilder,
		IAddonManagerInvalidator addonInvalidator)
	{
		SkillSettings = settings;
		_skillsetBuilder = skillsetBuilder;
		_addonInvalidator = addonInvalidator;

		RefreshSkillsCommand = new RelayCommand(UpdateSkills);
		CreateSkillCommand = new AsyncRelayCommand(CreateSkillAsync);

		UpdateSkills();
	}

	/// <summary>
	/// Refreshes the list of available skills from the <see cref="ISkillsetBuildingService"/>.
	/// </summary>
	public void UpdateSkills()
	{
		_addonInvalidator.Reload();

		_allCards.ForEach(c => c.Dispose());
		_allCards = _skillsetBuilder.GetAvailableAddons()
			.Select(s => new SkillCardViewModel(
				s,
				canToggle: false,
				onTagClick: tag => SearchText = tag,
				onDeleted: UpdateSkills))
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

		AvailableSkills = filtered.ToImmutableList();
	}

	private async Task CreateSkillAsync()
	{
		var dialog = new TextInputDialogViewModel
		{
			Title = LocalizationManager.LocalizeStatic("settings.skills.create.title"),
			Description = LocalizationManager.LocalizeStatic("settings.skills.create.description"),
			Label = LocalizationManager.LocalizeStatic("settings.skills.create.name.label"),
			Placeholder = LocalizationManager.LocalizeStatic("settings.skills.create.name.placeholder"),
			SubmitText = LocalizationManager.LocalizeStatic("common.create"),
			CancelText = LocalizationManager.LocalizeStatic("common.cancel"),
			IsRequired = true
		};

		var name = (string?)await DialogManager.ShowDialogAsync(dialog);
		if (string.IsNullOrEmpty(name))
			return;

		var toast = ServiceRegistry.Provider.GetRequiredService<IToastService>();
		if (!SkillName.IsValidSkillName(name))
		{
			toast.ShowError(LocalizationManager.LocalizeStatic("settings.skills.create.title"),
				LocalizationManager.LocalizeStatic("settings.skills.create.error.invalid_name"));
			return;
		}

		var directory = Path.Combine(Directories.Skills, name);
		var path = Path.Combine(directory, "SKILL.md");
		if (File.Exists(path))
		{
			toast.ShowError(LocalizationManager.LocalizeStatic("settings.skills.create.title"),
				LocalizationManager.LocalizeStatic("settings.skills.create.error.exists"));
			return;
		}

		try
		{
			Directory.CreateDirectory(directory);
			File.WriteAllText(path, BuildTemplate(name));
			UpdateSkills();

			toast.ShowSuccess(LocalizationManager.LocalizeStatic("settings.skills.create.success"));
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
		description: A skill that helps with specific tasks.
		---

		# {name}

		Write the instructions for this skill here.
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
