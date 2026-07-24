using System.Collections.ObjectModel;
using System.Text;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Messages;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Localization.Resources;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.Agents
{
	public class ToolItemViewModel : ViewModelBase
	{
		private readonly AgentToolSettings _settings;
		private readonly ToolInfo _toolInfo;
		private ToolChange? _change;

		public bool IsCategory => false;

		public IBrush? TitlePrefixForeground { get; }
		public string? TitlePrefix { get; }
		public string Title { get; }

		public ToolInfo Info => _toolInfo;
		public string Name { get; }
		public string Description { get; }
		public IBrush? DescriptionOpacityMask { get; }
		public string Category { get; }
		public ICommand ResetCommand { get; }

		/// <summary>
		/// Gets the list of behaviour flags with icons and colors for display.
		/// </summary>
		public IReadOnlyList<ToolBehaviourFlagInfo> BehaviourFlags { get; }

		public ToolItemViewModel(ToolInfo tool, AgentToolSettings settings)
		{
			_settings = settings;
			_toolInfo = tool;
			BehaviourFlags = ToolBehaviourFlagInfo.CreateForFlags(tool.DefaultExpectedBehaviour);

			switch (tool.Source)
			{
				case ToolSource.MCP:
					TitlePrefix = Locale.tool_source_mcp;
					TitlePrefixForeground = Brushes.LightGreen;
					break;

				case ToolSource.Meta:
					TitlePrefix = Locale.tool_source_meta;
					TitlePrefixForeground = Brushes.Magenta;
					break;
			}
			Title = tool.DisplayName ?? tool.Tool.Name;

			Name = tool.Tool.Name;
			Description = tool.Tool.Description;
			Category = tool.Category;
			ResetCommand = new RelayCommand(Reset);

			if (Description.Count(c => c == '\n') >= 5 || Description.Length > 750)
			{
				var gradientBrush = new LinearGradientBrush();
				gradientBrush.StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative);
				gradientBrush.EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative);
				gradientBrush.GradientStops.Add(new GradientStop(Colors.White, 0.5));
				gradientBrush.GradientStops.Add(new GradientStop(Colors.Transparent, 1.0));
				DescriptionOpacityMask = gradientBrush;
			}

			_change = _settings.ToolChanges.FirstOrDefault(x => x.ToolName == Name);
		}

		private void Reset()
		{
			if (_change != null)
			{
				_settings.ToolChanges.Remove(_change);
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
				_settings.ToolChanges.Add(_change);
			}
			return _change;
		}

		public bool? Enabled
		{
			get => _change?.Enabled ?? _toolInfo.Enabled;
			set
			{
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
		public string Title { get; }
		public string? TitleSuffix { get; }

		public int ToolCount => Tools.Count;

		/// <summary>
		/// Gets the list of approval levels from the first tool (all tools share the same static list).
		/// </summary>
		public IList<ToolApprovalLevelItem>? ApprovalLevelList => Tools.Count > 0 ? Tools[0].ApprovalLevelList : null;

		public ImmutableList<ToolItemViewModel> Tools { get; }
		public ICommand ResetCommand { get; }

		public ToolCategoryViewModel(string title, IEnumerable<ToolItemViewModel> tools)
		{
			Tools = tools.ToImmutableList();
			ResetCommand = new RelayCommand(ResetAllTools);

			Title = title;
			TitleSuffix = string.Format(Locale.tool_name_suffix_hint, ToolCount);

			if (Tools.Select(t => t.Info.Source).GetAllEqualOrDefault() is ToolSource equalSource)
			{
				switch (equalSource)
				{
					case ToolSource.MCP:
						TitlePrefix = Locale.tool_source_mcp;
						TitlePrefixForeground = Brushes.LightGreen;
						break;

					case ToolSource.Meta:
						TitlePrefix = Locale.tool_source_meta;
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
			get => Tools.All(t => t.Enabled == true) ? true : Tools.All(t => t.Enabled == false) ? false : null;
			set
			{
				if (Enabled != value)
					foreach (var tool in Tools)
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
		private readonly IToolsetBuildingService _toolsetBuildingService;
		public AgentToolSettings ToolSettings { get; }


	/// <summary>
	/// List of ToolBehaviour flags with combined Auto-Approve / Disallowed policy toggles.
	/// </summary>
	public ObservableCollection<ToolBehaviourPolicyItem> PolicyBehaviourItems { get; } = [];

		/// <summary>
		/// Whether to override the global tool policy for this agent.
		/// </summary>
		public bool EnablePolicyOverride
		{
			get => ToolSettings.EnablePolicyOverride;
			set
			{
				if (ToolSettings.EnablePolicyOverride != value)
				{
					ToolSettings.EnablePolicyOverride = value;
					RaisePropertyChanged();
				}
			}
		}

		private RangeObservableCollection<ToolCategoryViewModel> _toolCategories = [];
		public ICollection<ToolCategoryViewModel> ToolCategories
		{
			get => _toolCategories;
			set
			{
				_toolCategories.Reset(value);
				RaisePropertyChanged(nameof(ToolCategories));
			}
		}

		public AgentToolSettingsViewModel(AgentToolSettings settings, IToolsetBuildingService toolsetBuildingService)
		{
			_toolsetBuildingService = toolsetBuildingService;
			ToolSettings = settings;
			InitializeBehaviourItems();
			UpdateTools();
		}

	private void InitializeBehaviourItems()
		{
			PolicyBehaviourItems.Clear();

			foreach (var flag in GetBehaviourFlags())
			{
				var key = $"tool_behaviour_{flag.ToString().ToLower()}";
				var displayName = LocalizationManager.LocalizeStatic(key);
				var description = LocalizationManager.LocalizeStatic($"{key}_hint");

				// Fallback to CamelCase split if localization is missing
				if (displayName == key || string.IsNullOrEmpty(displayName))
					displayName = SplitCamelCase(flag.ToString());
				if (description == $"{key}_hint" || string.IsNullOrEmpty(description))
					description = string.Empty;

				PolicyBehaviourItems.Add(new ToolBehaviourPolicyItem(
					() => ToolSettings.AutoApproveBehaviours,
					v => ToolSettings.AutoApproveBehaviours = v,
					() => ToolSettings.DisallowedBehaviours,
					v => ToolSettings.DisallowedBehaviours = v,
					flag,
					displayName,
					description));
			}
		}

		private static IEnumerable<ToolBehaviour> GetBehaviourFlags()
		{
			return Enum.GetValues<ToolBehaviour>()
				.Where(v => v != ToolBehaviour.None);
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


		public void UpdateTools()
		{
			var tools = _toolsetBuildingService.GetAvailableTools();
			var toolVMs = tools.Select(t => new ToolItemViewModel(t, ToolSettings));

			foreach (var category in ToolCategories)
				category.Dispose();

			ToolCategories = toolVMs
				.GroupBy(t => t.Category)
				.Select(g => new ToolCategoryViewModel(g.Key, g))
				.ToImmutableList();
		}
	}
}
