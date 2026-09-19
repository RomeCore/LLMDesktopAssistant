using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Addons.Management;
using LLMDesktopAssistant.Addons.MVVM;
using LLMDesktopAssistant.Addons.Search;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Tools;

namespace LLMDesktopAssistant.LLM.MVVM.Settings;

/// <summary>
/// ViewModel for the chat-level tool settings: the available tools rendered by the reusable addon list
/// (with the search box). The list is read-only here, since there is no chat-level toolset: enabling
/// and configuring tools is done per agent in <see cref="Agents.AgentToolSettingsViewModel"/>.
/// </summary>
[ViewModelFor(typeof(ChatToolsSettingsView))]
public class ChatToolsSettingsViewModel : ViewModelBase
{
	/// <summary>
	/// Gets the addon list that renders the available tools and searches over them.
	/// </summary>
	public AddonListViewModel List { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ChatToolsSettingsViewModel"/> class.
	/// </summary>
	/// <param name="toolsetCollector">The collector that provides the available tools.</param>
	/// <param name="cardFactory">The factory that builds the tool cards.</param>
	/// <param name="addonInvalidator">The invalidator used to reload the addons before building the list.</param>
	/// <param name="searchService">The search service used to filter the list by the search query.</param>
	public ChatToolsSettingsViewModel(IAddonSetCollector<ToolInfo> toolsetCollector,
		IAddonCardFactory<ToolInfo, ToolChange> cardFactory,
		IAddonManagerInvalidator addonInvalidator,
		IAddonSearchService<ToolInfo> searchService)
	{
		List = new AddonListViewModel<ToolInfo, ToolChange>(toolsetCollector, cardFactory, addonInvalidator,
			AddonKind.Tool, searchService)
		{
			SearchPlaceholderKey = Locale.GetKey("settings.tools.search.placeholder"),
			EmptyTextKey = Locale.GetKey("settings.tools.empty")
		};
		List.Update();
	}

	/// <inheritdoc/>
	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);

		if (disposing)
			List.Dispose();
	}
}
