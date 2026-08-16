using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Messages;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.Agents
{
	public class ToolItemViewModel : ViewModelBase
	{
		private readonly ToolsetConfiguration _toolset;
		private readonly ToolInfo _toolInfo;
		private ToolChange? _change;

		public ToolInfo Info => _toolInfo;
		public string Name { get; }

		public bool IsCategory => false;
		public LocaleKeyBase Category { get; }

		public IBrush? TitlePrefixForeground { get; }
		public LocaleKeyBase? TitlePrefix { get; }

		public LocaleKeyBase Description { get; }
		public LocaleKeyBase Title { get; }

		public bool IsFixed => _toolInfo.IsFixed;
		public ICommand ResetCommand { get; }

		/// <summary>
		/// Gets the list of behaviour flags with icons and colors for display.
		/// </summary>
		public IReadOnlyList<ToolBehaviourFlagInfo> BehaviourFlags { get; }

		public ToolItemViewModel(ToolInfo tool, ToolsetConfiguration toolset)
		{
			_toolset = toolset;
			_toolInfo = tool;
			BehaviourFlags = ToolBehaviourFlagInfo.CreateForFlags(tool.DefaultExpectedBehaviour);

			switch (tool.Source)
			{
				case ToolSource.MCP:
					TitlePrefix = Locale.GetKey("tool.source.mcp");
					TitlePrefixForeground = Brushes.LightGreen;
					break;

				case ToolSource.Meta:
					TitlePrefix = Locale.GetKey("tool.source.meta");
					TitlePrefixForeground = Brushes.Magenta;
					break;
			}

			Name = tool.Name;
			Title = tool.TitleKey ?? Locale.GetConstKey(tool.Name);
			Description = tool.DescriptionKey ?? Locale.GetConstKey(tool.DescriptionGetter());
			Category = tool.CategoryKey ?? Locale.GetKey("tool.category.unknown");
			ResetCommand = new RelayCommand(Reset);

			_change = _toolset.ToolChanges.FirstOrDefault(x => x.ToolName == Name);
		}

		private void Reset()
		{
			if (_change != null)
			{
				_toolset.ToolChanges.Remove(_change);
				_change = null;
				RaisePropertyChanged(nameof(Enabled));
				RaisePropertyChanged(nameof(ApprovalLevel));
			}
		}

		private ToolChange EnsureChange()
		{
			if (_change == null)
			{
				_change = new ToolChange
				{
					ToolName = Name,
					Enabled = null,
					ApprovalLevel = null
				};
				_toolset.ToolChanges.Add(_change);
			}
			return _change;
		}

		public bool? Enabled
		{
			get => IsFixed ? true : (_change?.Enabled ?? _toolInfo.Enabled);
			set
			{
				if (IsFixed)
					return;
				if (Enabled != value)
				{
					EnsureChange().Enabled = value;
					RaisePropertyChanged(nameof(Enabled));
				}
			}
		}

		public ImmutableList<ToolApprovalLevelItem> ApprovalLevelList { get; } = ToolApprovalLevelItem.All;

		public ToolApprovalLevelItem? ApprovalLevel
		{
			get => ApprovalLevelList.FirstOrDefault(i => i.Value == (_change?.ApprovalLevel ?? _toolInfo.ApprovalLevel));
			set
			{
				if (ApprovalLevel != value)
				{
					EnsureChange().ApprovalLevel = value?.Value;
					RaisePropertyChanged(nameof(ApprovalLevel));
				}
			}
		}
	}

	public class ToolCategoryViewModel : ViewModelBase
	{
		public bool IsCategory => true;

		public IBrush? TitlePrefixForeground { get; }
		public string? TitlePrefix { get; }
		public LocaleKeyBase Title { get; }
		public string? TitleSuffix { get; }

		public int ToolCount => Tools.Count;

		public bool CanToggleEnabled => Tools.Any(t => !t.IsFixed);

		/// <summary>
		/// Gets the list of approval levels from the first tool (all tools share the same static list).
		/// </summary>
		public IList<ToolApprovalLevelItem>? ApprovalLevelList => Tools.Count > 0 ? Tools[0].ApprovalLevelList : null;

		public ImmutableList<ToolItemViewModel> Tools { get; }
		public ICommand ResetCommand { get; }

		public ToolCategoryViewModel(LocaleKeyBase title, IEnumerable<ToolItemViewModel> tools)
		{
			Tools = tools.ToImmutableList();
			ResetCommand = new RelayCommand(ResetAllTools);

			Title = title;
			TitleSuffix = string.Format(Locale.Get("tool.name_suffix.hint"), ToolCount);

			if (Tools.Select(t => t.Info.Source).GetAllEqualOrDefault() is ToolSource equalSource)
			{
				switch (equalSource)
				{
					case ToolSource.MCP:
						TitlePrefix = Locale.Get("tool.source.mcp");
						TitlePrefixForeground = Brushes.LightGreen;
						break;

					case ToolSource.Meta:
						TitlePrefix = Locale.Get("tool.source.meta");
						TitlePrefixForeground = Brushes.Magenta;
						break;
				}
			}

			foreach (var tool in Tools)
				tool.PropertyChanged += Tool_PropertyChanged;
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			foreach (var tool in Tools)
				tool.PropertyChanged -= Tool_PropertyChanged;
		}

		private void ResetAllTools()
		{
			foreach (var tool in Tools)
				tool.ResetCommand.Execute(null);
		}

		private void Tool_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(Enabled) || e.PropertyName == nameof(ApprovalLevel))
				RaisePropertyChanged(e.PropertyName);
		}

		public bool? Enabled
		{
			get => Tools.All(t => t.IsFixed) || Tools.Where(t => !t.IsFixed).All(t => t.Enabled == true) ? true : Tools.Where(t => !t.IsFixed).All(t => t.Enabled == false) ? false : null;
			set
			{
				if (Enabled != value)
					foreach (var tool in Tools)
						if (!tool.IsFixed)
							tool.Enabled = value;
			}
		}

		public ToolApprovalLevelItem? ApprovalLevel
		{
			get => Tools.All(t => t.ApprovalLevel == Tools[0].ApprovalLevel) ? Tools[0].ApprovalLevel : null;
			set
			{
				if (ApprovalLevel != value && value != null)
					foreach (var tool in Tools)
						tool.ApprovalLevel = value;
			}
		}
	}

	[ViewModelFor(typeof(AgentToolSettingsView))]
	public class AgentToolSettingsViewModel : ViewModelBase
	{
		private enum IdEditMode
		{
			Create,
			Rename
		}

		private readonly IToolsetBuildingService _toolsetBuildingService;
		private readonly ChatSettings _chatSettings;
		private IdEditMode _mode = IdEditMode.Create;

		/// <summary>
		/// Gets the underlying agent tool settings.
		/// </summary>
		public AgentToolSettings ToolSettings { get; }

		/// <summary>
		/// Gets the effective tool behaviour policy resolved by the current inheritance level.
		/// </summary>
		public ToolPolicySettings EffectivePolicy => ToolSettings.GetEffectivePolicy(_chatSettings);

		/// <summary>
		/// Gets the effective toolset settings resolved by the current inheritance level.
		/// </summary>
		public ToolsetSettings EffectiveToolset => ToolSettings.GetEffectiveToolset(_chatSettings);

		/// <summary>
		/// Gets the effective toolset configuration (custom or referenced shared).
		/// </summary>
		public ToolsetConfiguration EffectiveToolsetConfiguration => EffectiveToolset.GetEffectiveConfiguration();

		/// <summary>
		/// List of ToolBehaviour flags with combined Auto-Approve / Disallowed policy toggles.
		/// </summary>
		public ObservableCollection<ToolBehaviourPolicyItem> PolicyBehaviourItems { get; } = [];

		private InheritanceLevelItem _selectedPolicyInheritance;
		/// <summary>
		/// Gets or sets the inheritance level for the tool policy group.
		/// </summary>
		public InheritanceLevelItem SelectedPolicyInheritance
		{
			get => _selectedPolicyInheritance;
			set
			{
				if (SetProperty(ref _selectedPolicyInheritance, value) && value != null)
					ToolSettings.PolicyInheritance = value.Value;
			}
		}

		private InheritanceLevelItem _selectedToolsetInheritance;
		/// <summary>
		/// Gets or sets the inheritance level for the toolset group.
		/// </summary>
		public InheritanceLevelItem SelectedToolsetInheritance
		{
			get => _selectedToolsetInheritance;
			set
			{
				if (SetProperty(ref _selectedToolsetInheritance, value) && value != null)
					ToolSettings.ToolsetInheritance = value.Value;
			}
		}

		/// <summary>
		/// Gets the settings category that stores shared toolset configurations.
		/// </summary>
		public static SettingsCategory<ToolsetConfiguration> ToolsetCategory { get; } = SettingsManager.GetCategory<ToolsetConfiguration>();

		/// <summary>
		/// Gets the available shared toolset configuration IDs.
		/// </summary>
		public RangeObservableCollection<SettingsIdItemViewModel> ToolsetIds { get; } = [ ..ToolsetCategory.Ids
			.Where(c => c != SettingsObject.DefaultId)
			.Select(c => new SettingsIdItemViewModel { Id = c })
			.Prepend(SettingsIdItemViewModel.Default) ];

		private SettingsIdItemViewModel _selectedToolsetId = null!;
		/// <summary>
		/// Gets or sets the selected shared toolset configuration.
		/// </summary>
		public SettingsIdItemViewModel SelectedToolsetId
		{
			get => _selectedToolsetId;
			set
			{
				if (value == null)
					value = SettingsIdItemViewModel.Default;
				if (SetProperty(ref _selectedToolsetId, value))
				{
					EffectiveToolset.Reference.Id = value.Id;
					UpdateTools();
				}
			}
		}

		private bool _isEditingId;
		/// <summary>
		/// Gets or sets a value indicating whether the toolset ID editor is visible.
		/// </summary>
		public bool IsEditingId
		{
			get => _isEditingId;
			set => SetProperty(ref _isEditingId, value);
		}

		private string? _newId;
		/// <summary>
		/// Gets or sets the toolset ID being created or renamed.
		/// </summary>
		public string? NewId
		{
			get => _newId;
			set => SetProperty(ref _newId, value);
		}

		/// <summary>
		/// Gets or sets a value indicating whether the custom toolset is used instead of the referenced shared one.
		/// </summary>
		public bool UseCustomToolset
		{
			get => EffectiveToolset.UseCustomToolset;
			set
			{
				if (EffectiveToolset.UseCustomToolset != value)
				{
					EffectiveToolset.UseCustomToolset = value;
					RaisePropertyChanged();
					UpdateTools();
				}
			}
		}

		public ICommand CreateNewIdCommand { get; }
		public ICommand RenameIdCommand { get; }
		public ICommand RemoveIdCommand { get; }
		public ICommand ConfirmEditIdCommand { get; }
		public ICommand CancelEditIdCommand { get; }

		private RangeObservableCollection<ToolCategoryViewModel> _toolCategories = [];
		/// <summary>
		/// Gets the tool list grouped by categories for the effective toolset.
		/// </summary>
		public ICollection<ToolCategoryViewModel> ToolCategories
		{
			get => _toolCategories;
			set
			{
				_toolCategories.Reset(value);
				RaisePropertyChanged(nameof(ToolCategories));
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AgentToolSettingsViewModel"/> class.
		/// </summary>
		/// <param name="settings">The agent tool settings to edit.</param>
		/// <param name="toolsetBuildingService">The toolset building service used to enumerate available tools.</param>
		/// <param name="chatSettings">The chat settings used to resolve inherited settings.</param>
		public AgentToolSettingsViewModel(AgentToolSettings settings, IToolsetBuildingService toolsetBuildingService, ChatSettings chatSettings)
		{
			_toolsetBuildingService = toolsetBuildingService;
			_chatSettings = chatSettings;
			ToolSettings = settings;

			_selectedPolicyInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == settings.PolicyInheritance);
			_selectedToolsetInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == settings.ToolsetInheritance);
			_selectedToolsetId = ToolsetIds.FirstOrDefault(i => i.Id == EffectiveToolset.Reference.Id) ?? SettingsIdItemViewModel.Default;

			settings.PropertyChanged += ToolSettings_PropertyChanged;

			InitializeBehaviourItems();
			UpdateTools();

			CreateNewIdCommand = new RelayCommand(CreateNewId);
			RenameIdCommand = new RelayCommand(RenameId);
			RemoveIdCommand = new RelayCommand(RemoveId);
			ConfirmEditIdCommand = new RelayCommand(ConfirmEditId);
			CancelEditIdCommand = new RelayCommand(() => IsEditingId = false);
		}

		private void ToolSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(AgentToolSettings.PolicyInheritance):
					_selectedPolicyInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == ToolSettings.PolicyInheritance);
					RaisePropertyChanged(nameof(SelectedPolicyInheritance));
					RaisePropertyChanged(nameof(EffectivePolicy));
					InitializeBehaviourItems();
					break;

				case nameof(AgentToolSettings.ToolsetInheritance):
					_selectedToolsetInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == ToolSettings.ToolsetInheritance);
					_selectedToolsetId = ToolsetIds.FirstOrDefault(i => i.Id == EffectiveToolset.Reference.Id) ?? SettingsIdItemViewModel.Default;
					RaisePropertyChanged(nameof(SelectedToolsetInheritance));
					RaisePropertyChanged(nameof(EffectiveToolset));
					RaisePropertyChanged(nameof(EffectiveToolsetConfiguration));
					RaisePropertyChanged(nameof(SelectedToolsetId));
					RaisePropertyChanged(nameof(UseCustomToolset));
					UpdateTools();
					break;
			}
		}

		private void InitializeBehaviourItems()
		{
			PolicyBehaviourItems.Clear();

			foreach (var flag in GetBehaviourFlags())
			{
				var key = $"tool.behaviour.{flag.ToString().ToLower()}";
				var displayName = LocalizationManager.LocalizeStatic(key);
				var description = LocalizationManager.LocalizeStatic($"{key}_hint");

				// Fallback to CamelCase split if localization is missing
				if (displayName == key || string.IsNullOrEmpty(displayName))
					displayName = SplitCamelCase(flag.ToString());
				if (description == $"{key}_hint" || string.IsNullOrEmpty(description))
					description = string.Empty;

				PolicyBehaviourItems.Add(new ToolBehaviourPolicyItem(
					() => EffectivePolicy.AutoApproveBehaviours,
					v => EffectivePolicy.AutoApproveBehaviours = v,
					() => EffectivePolicy.DisallowedBehaviours,
					v => EffectivePolicy.DisallowedBehaviours = v,
					flag,
					displayName,
					description));
			}
		}

		private static IEnumerable<ToolBehaviour> GetBehaviourFlags()
		{
			return Enum.GetValues<ToolBehaviour>()
				.Where(v => v != ToolBehaviour.None && v != ToolBehaviour.All);
		}

		private static string SplitCamelCase(string input)
		{
			if (string.IsNullOrEmpty(input))
				return input;

			var result = new StringBuilder();
			for (int i = 0; i < input.Length; i++)
			{
				if (i > 0 && char.IsUpper(input[i]))
				{
					if (!char.IsUpper(input[i - 1]) || (i + 1 < input.Length && char.IsLower(input[i + 1])))
						result.Append(' ');
				}
				result.Append(input[i]);
			}
			return result.ToString();
		}

		/// <summary>
		/// Rebuilds the tool list from the effective toolset configuration.
		/// </summary>
		public void UpdateTools()
		{
			var tools = _toolsetBuildingService.GetAvailableTools();
			var toolVMs = tools.Select(t => new ToolItemViewModel(t, EffectiveToolsetConfiguration));

			foreach (var category in ToolCategories)
				category.Dispose();

			ToolCategories = toolVMs
				.GroupBy(t => t.Category)
				.Select(g => new ToolCategoryViewModel(g.Key, g))
				.ToImmutableList();
		}

		private void CreateNewId()
		{
			_mode = IdEditMode.Create;
			IsEditingId = true;
			NewId = null;
		}

		private void RenameId()
		{
			_mode = IdEditMode.Rename;
			IsEditingId = true;
			NewId = EffectiveToolset.Reference.Id;
		}

		private void RemoveId()
		{
			var currentId = EffectiveToolset.Reference.Id;
			if (ToolsetCategory.Remove(currentId))
			{
				if (currentId != SettingsObject.DefaultId)
					ToolsetIds.Remove(new SettingsIdItemViewModel { Id = currentId });
				SelectedToolsetId = SettingsIdItemViewModel.Default;
			}
		}

		private void ConfirmEditId()
		{
			var oldId = EffectiveToolset.Reference.Id;
			switch (_mode)
			{
				case IdEditMode.Create:

					if (!string.IsNullOrWhiteSpace(NewId) && ToolsetCategory.Copy(oldId, NewId))
					{
						if (NewId != SettingsObject.DefaultId && !ToolsetIds.Any(c => c.Id == NewId))
							ToolsetIds.Add(new SettingsIdItemViewModel { Id = NewId });

						SelectedToolsetId = new SettingsIdItemViewModel { Id = NewId };
						IsEditingId = false;
						NewId = null;
					}

					break;

				case IdEditMode.Rename:

					var newId = NewId == SettingsIdItemViewModel.Default.DisplayId ? SettingsObject.DefaultId : NewId;
					if (!string.IsNullOrWhiteSpace(newId) &&
						newId != oldId &&
						ToolsetCategory.Rename(oldId, newId))
					{
						if (newId != SettingsObject.DefaultId && !ToolsetIds.Any(c => c.Id == newId))
							ToolsetIds.Add(new SettingsIdItemViewModel { Id = newId });
						if (oldId != SettingsObject.DefaultId)
							ToolsetIds.Remove(new SettingsIdItemViewModel { Id = oldId });
						SelectedToolsetId = new SettingsIdItemViewModel { Id = newId };

						ToolsetCategory.Get(SettingsObject.DefaultId); // Ensure default settings are loaded if they were renamed.
						IsEditingId = false;
						NewId = null;
					}

					break;
			}
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
			{
				ToolSettings.PropertyChanged -= ToolSettings_PropertyChanged;

				foreach (var category in ToolCategories)
					category.Dispose();
				_toolCategories.Clear();
			}
		}
	}
}
