using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Core.Prompting;
using LLMDesktopAssistant.Core.Settings;
using Serilog;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;

namespace LLMDesktopAssistant.Avalonia.Prompting
{
	public class PromptComponentItemViewModel : ViewModelBase
	{
		public PromptComponent Component { get; }

		public PromptComponentItemViewModel(PromptComponent component)
		{
			Component = component;

			Component.SubscribeChanged(nameof(PromptComponent.Category),
				_ => this.RaisePropertyChanged(nameof(DisplayCategory)), out var subscribtion);
			OnDispose += (s, e) => subscribtion.Dispose();
		}

		public string DisplayCategory => string.IsNullOrEmpty(Component.Category)
			? LLMDesktopAssistant.Core.Localization.LocalizationManager.LocalizeStatic("prompt_category_uncategorized")
			: Component.Category;
	}

	public class PersonaItemViewModel : ViewModelBase
	{
		public Persona Persona { get; }

		public PersonaItemViewModel(Persona persona)
		{
			Persona = persona;
		}
	}

	[ViewModelFor(typeof(PromptManagerView))]
	public class PromptManagerViewModel : ViewModelBase
	{
		public PromptComponentsConfiguration ComponentsConfig { get; }
		public PersonasConfiguration PersonasConfig { get; }

		public ObservableCollection<PromptComponentItemViewModel> Components { get; }
		public ObservableCollection<PersonaItemViewModel> Personas { get; }

		private PromptComponentItemViewModel? _selectedComponent;
		public PromptComponentItemViewModel? SelectedComponent
		{
			get => _selectedComponent;
			set
			{
				if (SetProperty(ref _selectedComponent, value))
				{
					if (value != null)
						SelectedPersona = null;
					RemoveComponentCommand.NotifyCanExecuteChanged();
				}
			}
		}

		private PersonaItemViewModel? _selectedPersona;
		public PersonaItemViewModel? SelectedPersona
		{
			get => _selectedPersona;
			set
			{
				if (SetProperty(ref _selectedPersona, value))
				{
					if (value != null)
						SelectedComponent = null;
					RemovePersonaCommand.NotifyCanExecuteChanged();
				}
			}
		}

		public AsyncRelayCommand AddComponentCommand { get; }
		public RelayCommand RemoveComponentCommand { get; }
		public AsyncRelayCommand ImportComponentCommand { get; }

		public AsyncRelayCommand AddPersonaCommand { get; }
		public RelayCommand RemovePersonaCommand { get; }
		public AsyncRelayCommand ImportPersonaCommand { get; }

		public PromptManagerViewModel()
		{
			ComponentsConfig = SettingsManager.Get<PromptComponentsConfiguration>();
			PersonasConfig = SettingsManager.Get<PersonasConfiguration>();

			Components = new ObservableCollection<PromptComponentItemViewModel>(
				ComponentsConfig.Components.Select(c => new PromptComponentItemViewModel(c))
			);
			Personas = new ObservableCollection<PersonaItemViewModel>(
				PersonasConfig.Personas.Select(p => new PersonaItemViewModel(p))
			);

			AddComponentCommand = new AsyncRelayCommand(AddComponent);
			RemoveComponentCommand = new RelayCommand(RemoveComponent, () => SelectedComponent != null);
			ImportComponentCommand = new AsyncRelayCommand(ImportComponent);

			AddPersonaCommand = new AsyncRelayCommand(AddPersona);
			RemovePersonaCommand = new RelayCommand(RemovePersona, () => SelectedPersona != null);
			ImportPersonaCommand = new AsyncRelayCommand(ImportPersona);
		}

		private async Task AddComponent()
		{
			var component = new PromptComponent
			{
				Name = LLMDesktopAssistant.Core.Localization.LocalizationManager.LocalizeStatic("prompt_new_component"),
				Category = string.Empty,
				Text = string.Empty
			};

			ComponentsConfig.Components.Add(component);

			var vm = new PromptComponentItemViewModel(component);
			Components.Add(vm);
			SelectedComponent = vm;

			await EditComponent();
		}

		private void RemoveComponent()
		{
			if (SelectedComponent == null) return;

			ComponentsConfig.Components.Remove(SelectedComponent.Component);
			Components.Remove(SelectedComponent);
			SelectedComponent = null;
		}

		private async Task EditComponent()
		{
			if (SelectedComponent == null) return;

			// TODO: открыть диалог редактирования компонента
			await Task.CompletedTask;
		}

		private async Task ImportComponent()
		{
			var files = await App.MainTopLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
			{
				Title = LLMDesktopAssistant.Core.Localization.LocalizationManager.LocalizeStatic("prompt_import_component"),
				FileTypeFilter = new[]
				{
					new FilePickerFileType("Text files") { Patterns = new[] { "*.txt", "*.md" } },
					new FilePickerFileType("All files") { Patterns = new[] { "*" } }
				},
				AllowMultiple = true
			});

			foreach (var file in files)
			{
				try
				{
					var text = await File.ReadAllTextAsync(file.Path.LocalPath);
					var component = new PromptComponent
					{
						Name = Path.GetFileNameWithoutExtension(file.Name),
						Category = string.Empty,
						Text = text
					};

					ComponentsConfig.Components.Add(component);
					Components.Add(new PromptComponentItemViewModel(component));
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Failed to import component from file: {Path}", file.Path.LocalPath);
				}
			}
		}

		private async Task AddPersona()
		{
			var persona = new Persona
			{
				Name = LLMDesktopAssistant.Core.Localization.LocalizationManager.LocalizeStatic("prompt_new_persona"),
				Text = string.Empty
			};

			PersonasConfig.Personas.Add(persona);

			var vm = new PersonaItemViewModel(persona);
			Personas.Add(vm);
			SelectedPersona = vm;
		}

		private void RemovePersona()
		{
			if (SelectedPersona == null) return;

			PersonasConfig.Personas.Remove(SelectedPersona.Persona);
			Personas.Remove(SelectedPersona);
			SelectedPersona = null;
		}

		private async Task ImportPersona()
		{
			var files = await App.MainTopLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
			{
				Title = LLMDesktopAssistant.Core.Localization.LocalizationManager.LocalizeStatic("prompt_import_persona"),
				FileTypeFilter = new[]
				{
					new FilePickerFileType("Text files") { Patterns = new[] { "*.txt", "*.md" } },
					new FilePickerFileType("All files") { Patterns = new[] { "*" } }
				},
				AllowMultiple = true
			});

			foreach (var file in files)
			{
				try
				{
					var text = await File.ReadAllTextAsync(file.Path.LocalPath);
					var persona = new Persona
					{
						Name = Path.GetFileNameWithoutExtension(file.Name),
						Text = text
					};

					PersonasConfig.Personas.Add(persona);
					Personas.Add(new PersonaItemViewModel(persona));
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Failed to import persona from file: {Path}", file.Path.LocalPath);
				}
			}
		}
	}
}