using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Addons.Management;
using LLMDesktopAssistant.Addons.MVVM;
using LLMDesktopAssistant.Addons.Search;
using LLMDesktopAssistant.Agents.SubAgents;
using LLMDesktopAssistant.Controls.Dialogs;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.MVVM.Settings;

/// <summary>
/// ViewModel for the chat-level sub-agent settings: the available sub-agents rendered by the reusable
/// addon list (with the search box) and the sub-agent file actions.
/// </summary>
[ViewModelFor(typeof(ChatSubAgentsSettingsView))]
public class ChatSubAgentsSettingsViewModel : ViewModelBase
{
	/// <summary>
	/// Gets the underlying chat sub-agent settings.
	/// </summary>
	public ChatSubAgentSettings SubAgentSettings { get; }

	/// <summary>
	/// Gets the addon list that renders the available sub-agents and searches over them.
	/// </summary>
	public AddonListViewModel List { get; }

	/// <summary>
	/// Gets the command that creates a new sub-agent file from a template.
	/// </summary>
	public ICommand CreateSubAgentCommand { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ChatSubAgentsSettingsViewModel"/> class.
	/// </summary>
	/// <param name="settings">The chat sub-agent settings.</param>
	/// <param name="subAgentsetCollector">The collector that provides the available sub-agents.</param>
	/// <param name="cardFactory">The factory that builds the sub-agent cards.</param>
	/// <param name="addonInvalidator">The invalidator used to reload the addons before building the list.</param>
	/// <param name="searchService">The search service used to filter the list by the search query.</param>
	public ChatSubAgentsSettingsViewModel(ChatSubAgentSettings settings,
		IAddonSetCollector<SubAgentInfo> subAgentsetCollector,
		IAddonCardFactory<SubAgentInfo, SubAgentChange> cardFactory,
		IAddonManagerInvalidator addonInvalidator,
		IAddonSearchService<SubAgentInfo> searchService)
	{
		SubAgentSettings = settings;
		CreateSubAgentCommand = new AsyncRelayCommand(CreateSubAgentAsync);

		List = new AddonListViewModel<SubAgentInfo, SubAgentChange>(subAgentsetCollector, cardFactory, addonInvalidator, searchService)
		{
			SearchPlaceholderKey = Locale.GetKey("settings.sub_agents.search.placeholder"),
			EmptyTextKey = Locale.GetKey("settings.sub_agents.empty")
		};
		List.Update();
	}

	private async Task CreateSubAgentAsync()
	{
		var dialog = new TextInputDialogViewModel
		{
			Title = Locale.Get("settings.sub_agents.create.title"),
			Description = Locale.Get("settings.sub_agents.create.description"),
			Label = Locale.Get("settings.sub_agents.create.name.label"),
			Placeholder = Locale.Get("settings.sub_agents.create.name.placeholder"),
			SubmitText = Locale.Get("common.create"),
			CancelText = Locale.Get("common.cancel"),
			IsRequired = true
		};

		var name = (string?)await DialogManager.ShowDialogAsync(dialog);
		if (string.IsNullOrEmpty(name))
			return;

		var toast = ServiceRegistry.Provider.GetRequiredService<IToastService>();
		if (!SubAgentName.IsValidSubAgentName(name))
		{
			toast.ShowError(Locale.Get("settings.sub_agents.create.title"), Locale.Get("settings.sub_agents.create.error.invalid_name"));
			return;
		}

		var path = Path.Combine(Directories.Agents, $"{name}.md");
		if (File.Exists(path))
		{
			toast.ShowError(Locale.Get("settings.sub_agents.create.title"), Locale.Get("settings.sub_agents.create.error.exists"));
			return;
		}

		try
		{
			Directory.CreateDirectory(Directories.Agents);
			File.WriteAllText(path, BuildTemplate(name));
			List.Update();

			toast.ShowSuccess(Locale.Get("settings.sub_agents.create.success"));
			Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
		}
		catch (Exception ex)
		{
			toast.ShowError(Locale.Get("common.error"), ex.Message);
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
			List.Dispose();
	}
}
