using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Messages;
using LLMDesktopAssistant.LLM.Services.Prompting;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Prompting.Skills;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.Agents;

/// <summary>
/// ViewModel item for a single skill in the agent skills settings list.
/// Provides enabled toggle, injection mode selector, and reset command.
/// </summary>
public class SkillChangeItemViewModel : ViewModelBase
{
	private readonly AgentSkillSettings _settings;
	private readonly SkillInfo _skillInfo;
	private readonly RangeObservableCollection<SkillChange> _changes;
	private SkillChange? _change;

	/// <summary>
	/// Gets the name of the skill.
	/// </summary>
	public string Name => _skillInfo.Name;

	/// <summary>
	/// Gets the description of the skill.
	/// </summary>
	public string Description => _skillInfo.Description;

	/// <summary>
	/// Gets the file path of the skill, if applicable.
	/// </summary>
	public string? Path => _skillInfo.Path;

	/// <summary>
	/// Gets the list of diagnostic flag infos for display in the UI.
	/// </summary>
	public ImmutableList<SkillDiagnosticFlagInfo> DiagnosticFlags { get; }

	/// <summary>
	/// Gets the list of available injection modes for the ComboBox.
	/// </summary>
	public ImmutableList<SkillInjectionModeItem> InjectionModeList { get; } = SkillInjectionModeItem.All;

	/// <summary>
	/// Gets the command to reset skill changes to default values.
	/// </summary>
	public ICommand ResetCommand { get; }

	public SkillChangeItemViewModel(SkillInfo skillInfo, SkillChange? existingChange,
		RangeObservableCollection<SkillChange> changes, AgentSkillSettings settings)
	{
		_skillInfo = skillInfo;
		_change = existingChange;
		_changes = changes;
		_settings = settings;
		DiagnosticFlags = SkillDiagnosticFlagInfo.CreateFromDiagnostic(skillInfo.Diagnostic);
		ResetCommand = new RelayCommand(Reset);
	}

	private void Reset()
	{
		if (_change != null)
		{
			_changes.Remove(_change);
			_change = null;
			RaisePropertyChanged(nameof(Enabled));
			RaisePropertyChanged(nameof(InjectionMode));
		}
	}

	private SkillChange EnsureChange()
	{
		if (_change == null)
		{
			_change = new SkillChange
			{
				SkillName = Name,
				Enabled = null,
				InjectionMode = null
			};
			_changes.Add(_change);
		}
		return _change;
	}

	/// <summary>
	/// Gets or sets whether this skill is enabled. Returns <see langword="null"/> for inherited (default) value.
	/// </summary>
	public bool? Enabled
	{
		get => _change?.Enabled ?? _skillInfo.Enabled;
		set
		{
			if (Enabled != value)
			{
				EnsureChange().Enabled = value;
				RaisePropertyChanged(nameof(Enabled));
			}
		}
	}

	/// <summary>
	/// Gets or sets the injection mode for this skill.
	/// </summary>
	public SkillInjectionModeItem? InjectionMode
	{
		get => InjectionModeList.FirstOrDefault(i =>
			i.Value == (_change?.InjectionMode ?? _skillInfo.InjectionMode));
		set
		{
			if (InjectionMode != value && value != null)
			{
				EnsureChange().InjectionMode = value.Value;
				RaisePropertyChanged(nameof(InjectionMode));
			}
		}
	}
}

/// <summary>
/// Represents a <see cref="SkillInjectionMode"/> value with a localized display name for use in ComboBox.
/// </summary>
public class SkillInjectionModeItem
{
	/// <summary>
	/// The <see cref="SkillInjectionMode"/> value.
	/// </summary>
	public SkillInjectionMode Value { get; }

	/// <summary>
	/// Localized display name.
	/// </summary>
	public string DisplayName { get; }

	public SkillInjectionModeItem(SkillInjectionMode value)
	{
		Value = value;
		var key = $"skill.injection_mode.{value.ToString().ToLower()}";
		DisplayName = LocalizationManager.LocalizeStatic(key);

		// Fallback to enum name if localization missing
		if (DisplayName == key || string.IsNullOrEmpty(DisplayName))
			DisplayName = value.ToString();
	}

	/// <summary>
	/// Gets all <see cref="SkillInjectionMode"/> values with localized display names.
	/// </summary>
	public static ImmutableList<SkillInjectionModeItem> All { get; } =
		Enum.GetValues<SkillInjectionMode>()
			.Select(v => new SkillInjectionModeItem(v))
			.ToImmutableList();
}

/// <summary>
/// ViewModel for per-agent skills settings.
/// </summary>
[ViewModelFor(typeof(AgentSkillSettingsView))]
public class AgentSkillSettingsViewModel : ViewModelBase
{
	private readonly ISkillsetBuildingService _skillsetBuilder;
	private readonly ChatSettings _chatSettings;

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

	private RangeObservableCollection<SkillChangeItemViewModel> _skillItems = [];
	/// <summary>
	/// Gets or sets the list of skill items with per-agent override settings.
	/// </summary>
	public ICollection<SkillChangeItemViewModel> SkillItems
	{
		get => _skillItems;
		set
		{
			_skillItems.Reset(value);
			RaisePropertyChanged(nameof(SkillItems));
		}
	}

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
	/// Refreshes the list of available skills and rebuilds the skill items
	/// with current per-agent overrides.
	/// </summary>
	public void UpdateSkills()
	{
		var allSkills = _skillsetBuilder.GetAvailableSkills();
		var changes = EffectiveSkillChanges.ToDictionary(c => c.SkillName, c => c);

		SkillItems = allSkills
			.Where(s => s.Diagnostic?.IsFatal != true)
			.Select(s =>
			{
				changes.TryGetValue(s.Name, out var existingChange);
				return new SkillChangeItemViewModel(s, existingChange,
					EffectiveSkillChanges, SkillSettings);
			})
			.ToImmutableList();
	}

	/// <inheritdoc/>
	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);

		if (disposing)
			SkillSettings.PropertyChanged -= SkillSettings_PropertyChanged;
	}
}
