using Avalonia;
using Avalonia.Collections;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.Localization.Resources;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Settings
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
		public string? TitleSuffix { get; }

		public ToolInfo Info => _toolInfo;
		public string Name { get; }
		public string Description { get; }
		public IBrush? DescriptionOpacityMask { get; }
		public string Category { get; }
		public ICommand ResetCommand { get; }

		public ToolItemViewModel(ToolInfo tool, AgentToolSettings settings)
		{
			_settings = settings;
			_toolInfo = tool;

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
				RaisePropertyChanged(nameof(AskForConfirmation));
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
					AskForConfirmation = null
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

		public bool? AskForConfirmation
		{
			get => _change?.AskForConfirmation ?? _toolInfo.AskForConfirmation;
			set
			{
				if (AskForConfirmation != value)
				{
					EnsureChange().AskForConfirmation = value;
					RaisePropertyChanged(nameof(AskForConfirmation));
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
			if (e.PropertyName == nameof(Enabled) || e.PropertyName == nameof(AskForConfirmation))
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

		public bool? AskForConfirmation
		{
			get => Tools.All(t => t.AskForConfirmation == true) ? true : Tools.All(t => t.AskForConfirmation == false) ? false : null;
			set
			{
				if (AskForConfirmation != value)
					foreach (var tool in Tools)
						tool.AskForConfirmation = value;
			}
		}
	}

	[ViewModelFor(typeof(AgentToolSettingsView))]
	public class AgentToolSettingsViewModel : ViewModelBase
	{
		private readonly IToolsetBuildingService _toolsetBuildingService;
		public AgentToolSettings ToolSettings { get; }

		private AvaloniaList<ToolCategoryViewModel> _toolCategories = [];
		public ICollection<ToolCategoryViewModel> ToolCategories
		{
			get => _toolCategories;
			set
			{
				_toolCategories.Clear();
				_toolCategories.AddRange(value);
				RaisePropertyChanged(nameof(ToolCategories));
			}
		}

		public AgentToolSettingsViewModel(AgentToolSettings settings, IToolsetBuildingService toolsetBuildingService)
		{
			_toolsetBuildingService = toolsetBuildingService;
			ToolSettings = settings;
			UpdateTools();
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
