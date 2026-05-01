using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Prompting;
using LLMDesktopAssistant.Settings;
using Serilog;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;

namespace LLMDesktopAssistant.Prompting
{
	public class PromptComponentItemViewModel : ViewModelBase
	{
		public PromptComponent Component { get; }

		public string Text
		{
			get => Component.Template.SourceCode;
			set => Component.Template = new SerializableTextTemplate(value, TextTemplateType.PlainText);
		}

		public PromptComponentItemViewModel(PromptComponent component)
		{
			Component = component;

			Component.SubscribeChanged(nameof(PromptComponent.Category),
				_ => this.RaisePropertyChanged(nameof(DisplayCategory)), out var subscribtion);
			OnDispose += (s, e) => subscribtion.Dispose();
		}

		public string DisplayCategory => string.IsNullOrEmpty(Component.Category)
			? LLMDesktopAssistant.Localization.LocalizationManager.LocalizeStatic("prompt_category_uncategorized")
			: Component.Category;
	}

	public class PersonaItemViewModel : ViewModelBase
	{
		public Persona Persona { get; }

		public string Text
		{
			get => Persona.Template.SourceCode;
			set => Persona.Template = new SerializableTextTemplate(value, TextTemplateType.PlainText);
		}

		public PersonaItemViewModel(Persona persona)
		{
			Persona = persona;
		}
	}

	public class SpecializationItemViewModel : ViewModelBase
	{
		public Specialization Specialization { get; }

		public string Text
		{
			get => Specialization.Template.SourceCode;
			set => Specialization.Template = new SerializableTextTemplate(value, TextTemplateType.PlainText);
		}

		public SpecializationItemViewModel(Specialization specialization)
		{
			Specialization = specialization;
		}
	}

	[ViewModelFor(typeof(PromptManagerView))]
	public class PromptManagerViewModel : ViewModelBase
	{
		public PromptComponentsConfiguration ComponentsConfig { get; }
		public SpecializationsConfiguration SpecializationsConfig { get; }
		public PersonasConfiguration PersonasConfig { get; }

		public ObservableCollection<PromptComponentItemViewModel> Components { get; }
		public ObservableCollection<SpecializationItemViewModel> Specializations { get; }
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
					{
						SelectedPersona = null;
						SelectedSpecialization = null;
					}
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
					{
						SelectedComponent = null;
						SelectedSpecialization = null;
					}
					RemovePersonaCommand.NotifyCanExecuteChanged();
				}
			}
		}

		private SpecializationItemViewModel? _selectedSpecialization;
		public SpecializationItemViewModel? SelectedSpecialization
		{
			get => _selectedSpecialization;
			set
			{
				if (SetProperty(ref _selectedSpecialization, value))
				{
					if (value != null)
					{
						SelectedComponent = null;
						SelectedPersona = null;
					}
					RemoveSpecializationCommand.NotifyCanExecuteChanged();
				}
			}
		}

		public AsyncRelayCommand AddComponentCommand { get; }
		public RelayCommand RemoveComponentCommand { get; }
		public AsyncRelayCommand ImportComponentCommand { get; }

		public AsyncRelayCommand AddPersonaCommand { get; }
		public RelayCommand RemovePersonaCommand { get; }
		public AsyncRelayCommand ImportPersonaCommand { get; }

		public AsyncRelayCommand AddSpecializationCommand { get; }
		public RelayCommand RemoveSpecializationCommand { get; }
		public AsyncRelayCommand ImportSpecializationCommand { get; }

		public PromptManagerViewModel()
		{
			ComponentsConfig = SettingsManager.Get<PromptComponentsConfiguration>();
			SpecializationsConfig = SettingsManager.Get<SpecializationsConfiguration>();
			PersonasConfig = SettingsManager.Get<PersonasConfiguration>();

			Components = new ObservableCollection<PromptComponentItemViewModel>(
				ComponentsConfig.Components.Select(c => new PromptComponentItemViewModel(c))
			);
			Specializations = new ObservableCollection<SpecializationItemViewModel>(
				SpecializationsConfig.Specializations.Select(s => new SpecializationItemViewModel(s))
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

			AddSpecializationCommand = new AsyncRelayCommand(AddSpecialization);
			RemoveSpecializationCommand = new RelayCommand(RemoveSpecialization, () => SelectedSpecialization != null);
			ImportSpecializationCommand = new AsyncRelayCommand(ImportSpecialization);
		}

		private async Task AddComponent()
		{
			var component = new PromptComponent
			{
				Name = LLMDesktopAssistant.Localization.LocalizationManager.LocalizeStatic("prompt_new_component"),
				Category = string.Empty,
				Template = SerializableTextTemplate.Empty
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
				Title = LLMDesktopAssistant.Localization.LocalizationManager.LocalizeStatic("prompt_import_component"),
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
						Template = SerializableTextTemplate.Empty
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
				Name = LLMDesktopAssistant.Localization.LocalizationManager.LocalizeStatic("prompt_new_persona"),
				Template = SerializableTextTemplate.Empty
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
				Title = LLMDesktopAssistant.Localization.LocalizationManager.LocalizeStatic("prompt_import_persona"),
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
						Template = new SerializableTextTemplate(text, TextTemplateType.PlainText)
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

		private async Task AddSpecialization()
		{
			var specialization = new Specialization
			{
				Name = LLMDesktopAssistant.Localization.LocalizationManager.LocalizeStatic("prompt_new_specialization"),
				Category = string.Empty,
				Template = SerializableTextTemplate.Empty
			};

			SpecializationsConfig.Specializations.Add(specialization);

			var vm = new SpecializationItemViewModel(specialization);
			Specializations.Add(vm);
			SelectedSpecialization = vm;
		}

		private void RemoveSpecialization()
		{
			if (SelectedSpecialization == null) return;

			SpecializationsConfig.Specializations.Remove(SelectedSpecialization.Specialization);
			Specializations.Remove(SelectedSpecialization);
			SelectedSpecialization = null;
		}

		private async Task ImportSpecialization()
		{
			var files = await App.MainTopLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
			{
				Title = LLMDesktopAssistant.Localization.LocalizationManager.LocalizeStatic("prompt_import_specialization"),
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
					var specialization = new Specialization
					{
						Name = Path.GetFileNameWithoutExtension(file.Name),
						Category = string.Empty,
						Template = new SerializableTextTemplate(text, TextTemplateType.PlainText)
					};

					SpecializationsConfig.Specializations.Add(specialization);
					Specializations.Add(new SpecializationItemViewModel(specialization));
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Failed to import specialization from file: {Path}", file.Path.LocalPath);
				}
			}
		}
	}
}
