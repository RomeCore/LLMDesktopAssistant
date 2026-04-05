using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Localization.Resources;
using LLMDesktopAssistant.MVVM;
using LLMDesktopAssistant.ToolModules;
using LLMDesktopAssistant.Utils;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LLMDesktopAssistant.LLM.MVVM.Settings
{
	public class ToolAdditionalTemplateSelector : DataTemplateSelector
	{
		public DataTemplate? ToolTemplate { get; set; }

		public override DataTemplate? SelectTemplate(object item, DependencyObject container)
		{
			if (item is ToolItemViewModel)
				return ToolTemplate;

			return TreeViewAssist.SuppressAdditionalTemplate;
		}
	}

	public class ToolItemViewModel : ViewModelBase
	{
		private readonly ChatSettings _settings;
		private readonly ToolInfo _toolInfo;
		private ToolChange? _change;

		public string Name { get; }
		public string? NameSuffix => null;
		public string Description { get; }
		public Brush? DescriptionOpacityMask { get; }
		public string Category { get; }
		public ICommand ResetCommand { get; }

		public bool IsCategory => false;

		public ToolItemViewModel(ToolInfo tool, ChatSettings settings)
		{
			_settings = settings;
			_toolInfo = tool;
			Name = tool.DisplayName ?? tool.Tool.Name;
			Description = tool.Tool.Description;
			Category = tool.Category;
			ResetCommand = new RelayCommand(Reset);

			if (Description.Count(c => c == '\n') >= 5 || Description.Length > 750)
			{
				var gradientBrush = new LinearGradientBrush();
				gradientBrush.StartPoint = new Point(0, 0);
				gradientBrush.EndPoint = new Point(0, 1);
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
		public string Name { get; }
		public string NameSuffix => string.Format(Locale.tool_name_suffix_hint, ToolCount);
		public ImmutableList<ToolItemViewModel> Tools { get; }
		public ICommand ResetCommand { get; }

		public int ToolCount => Tools.Count;
		public bool IsCategory => true;

		public ToolCategoryViewModel(string name, IEnumerable<ToolItemViewModel> tools)
		{
			Name = name;
			Tools = tools.ToImmutableList();
			ResetCommand = new RelayCommand(ResetAllTools);

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

	[ViewModelFor(typeof(ChatToolSettingsView))]
	public class ChatToolSettingsViewModel : ViewModelBase
	{
		public ChatSettingsViewModel Parent { get; }
		public ChatSettings Settings { get; }

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

		public ChatToolSettingsViewModel(ChatSettingsViewModel parent)
		{
			Parent = parent;
			Settings = parent.Settings;
			UpdateTools();
		}

		public void UpdateTools()
		{
			var toolBuilder = Parent.Chat.Services.GetRequiredService<IToolsetBuildingService>();
			var tools = toolBuilder.GetAvailableTools();
			var toolVMs = tools.Select(t => new ToolItemViewModel(t, Settings));

			foreach (var category in ToolCategories)
				category.Dispose();

			ToolCategories = toolVMs
				.GroupBy(t => t.Category)
				.Select(g => new ToolCategoryViewModel(g.Key, g))
				.ToImmutableList();
		}
	}
}