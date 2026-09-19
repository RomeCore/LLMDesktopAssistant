using System.ComponentModel;
using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Addons.Management;
using LLMDesktopAssistant.Addons.MVVM;
using LLMDesktopAssistant.Addons.Search;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Scripting.Lua;

namespace LLMDesktopAssistant.LLM.MVVM.Settings;

/// <summary>
/// ViewModel for the chat-level Lua script settings: the effective scriptset selection (with its
/// inheritance level), the defaults of the scriptset and the available scripts rendered by the
/// reusable addon list, where every card edits the script overrides of the effective scriptset.
/// </summary>
[ViewModelFor(typeof(ChatScriptsSettingsView))]
public class ChatScriptsSettingsViewModel : ViewModelBase
{
	private readonly ChatScriptSettings _scriptSettings;

	private InheritanceLevelItem _selectedScriptsetInheritance;

	/// <summary>
	/// Gets the underlying chat script settings.
	/// </summary>
	public ChatScriptSettings ScriptSettings => _scriptSettings;

	/// <summary>
	/// Gets the addon list that renders the available Lua scripts and searches over them. The cards of
	/// the list edit the changes of <see cref="EffectiveScriptset"/>.
	/// </summary>
	public AddonListViewModel List { get; }

	/// <summary>
	/// Gets the scriptset resolved by the current inheritance level. The cards edit the changes of this set.
	/// </summary>
	public LuaScriptsetSettings EffectiveScriptset => _scriptSettings.GetEffectiveScriptset();

	/// <summary>
	/// Gets or sets the inheritance level of the scriptset.
	/// </summary>
	public InheritanceLevelItem SelectedScriptsetInheritance
	{
		get => _selectedScriptsetInheritance;
		set
		{
			if (SetProperty(ref _selectedScriptsetInheritance, value) && value is not null)
				_scriptSettings.ScriptsetInheritance = value.Value;
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ChatScriptsSettingsViewModel"/> class.
	/// </summary>
	/// <param name="settings">The chat script settings.</param>
	/// <param name="collector">The collector that provides the available Lua scripts.</param>
	/// <param name="cardFactory">The factory that builds the Lua script cards.</param>
	/// <param name="addonInvalidator">The invalidator used to reload the addons before building the list.</param>
	/// <param name="searchService">The search service used to filter the list by the search query.</param>
	public ChatScriptsSettingsViewModel(ChatScriptSettings settings,
		IAddonSetCollector<LuaScriptInfo> collector,
		IAddonCardFactory<LuaScriptInfo, LuaScriptChange> cardFactory,
		IAddonManagerInvalidator addonInvalidator,
		IAddonSearchService<LuaScriptInfo> searchService)
	{
		_scriptSettings = settings;
		_selectedScriptsetInheritance = InheritanceLevelItem.AllProfile.First(item => item.Value == settings.ScriptsetInheritance);
		settings.PropertyChanged += ScriptSettings_PropertyChanged;

		List = new AddonListViewModel<LuaScriptInfo, LuaScriptChange>(collector, cardFactory, addonInvalidator,
			AddonKind.LuaScript, searchService, (list, addon) => new AddonCardContext<LuaScriptInfo, LuaScriptChange>
			{
				Addon = addon,
				SetConfig = EffectiveScriptset,
				TagClickCommand = list.TagClickCommand,
				OnDeleted = list.Update
			})
		{
			SearchPlaceholderKey = Locale.GetKey("settings.scripts.search.placeholder"),
			EmptyTextKey = Locale.GetKey("settings.scripts.empty")
		};
		List.Update();
	}

	private void ScriptSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName != nameof(ChatScriptSettings.ScriptsetInheritance))
			return;

		_selectedScriptsetInheritance = InheritanceLevelItem.AllProfile.First(item => item.Value == _scriptSettings.ScriptsetInheritance);
		RaisePropertyChanged(nameof(SelectedScriptsetInheritance));
		RaisePropertyChanged(nameof(EffectiveScriptset));
		List.Update();
	}

	/// <inheritdoc/>
	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);

		if (disposing)
		{
			_scriptSettings.PropertyChanged -= ScriptSettings_PropertyChanged;
			List.Dispose();
		}
	}
}
