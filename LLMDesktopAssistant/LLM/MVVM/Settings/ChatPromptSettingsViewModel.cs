using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Prompting;
using LLMDesktopAssistant.Settings;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace LLMDesktopAssistant.LLM.Settings
{
	public class ComponentItemViewModel : ObservableObject
	{
		private readonly ChatPromptSettingsViewModel _parent;
		public PromptComponent Component { get; }

		public ComponentItemViewModel(ChatPromptSettingsViewModel parent, PromptComponent component)
		{
			_parent = parent;
			Component = component;
			_isSelected = parent.PromptSettings.PromptComponents.Contains(component.Id);
		}

		private bool _isSelected;
		public bool IsSelected
		{
			get => _isSelected;
			set
			{
				if (SetProperty(ref _isSelected, value))
				{
					_parent.UpdateSelectedComponents();
				}
			}
		}
	}

	public class ComponentCategoryViewModel : ObservableObject
	{
		public string CategoryName { get; }
		public ObservableCollection<ComponentItemViewModel> Components { get; } = new();

		public ComponentCategoryViewModel(string categoryName)
		{
			CategoryName = categoryName;
		}
	}

	public class PersonaItemViewModel : ObservableObject
	{
		private readonly ChatPromptSettingsViewModel _parent;
		public Persona Persona { get; }

		public PersonaItemViewModel(ChatPromptSettingsViewModel parent, Persona persona)
		{
			_parent = parent;
			Persona = persona;
		}
	}

	[ViewModelFor(typeof(ChatPromptSettingsView))]
	public class ChatPromptSettingsViewModel : ViewModelBase
	{
		public ChatPromptSettings PromptSettings { get; }

		public ObservableCollection<ComponentCategoryViewModel> ComponentCategories { get; } = new();
		public ObservableCollection<PersonaItemViewModel> AvailablePersonas { get; } = new();
		private PersonaItemViewModel? _selectedPersona;
		public PersonaItemViewModel? SelectedPersona
		{
			get => _selectedPersona;
			set
			{
				if (SetProperty(ref _selectedPersona, value))
				{
					if (value != null)
						PromptSettings.PersonaId = value.Persona.Id;
					else
						PromptSettings.PersonaId = null;
				}
			}
		}

		public ICommand ClearPersonaCommand { get; }

		public ChatPromptSettingsViewModel(ChatPromptSettings settings)
		{
			PromptSettings = settings;

			ClearPersonaCommand = new RelayCommand(() => SelectedPersona = null);
			Refresh();
		}

		public void Refresh()
		{
			var allComponents = new List<PromptComponent>();
			var componentsConfig = SettingsManager.Get<PromptComponentsConfiguration>();
			allComponents.AddRange(componentsConfig.Components);
			allComponents.AddRange(PromptRegistry.BuiltinComponents.Values);

			var grouped = allComponents.GroupBy(c => string.IsNullOrEmpty(c.Category) 
				? LLMDesktopAssistant.Localization.LocalizationManager.LocalizeStatic("prompt_category_uncategorized") 
				: c.Category);

			ComponentCategories.Clear();
			foreach (var group in grouped.OrderBy(g => g.Key))
			{
				var categoryVm = new ComponentCategoryViewModel(group.Key);
				foreach (var component in group.OrderBy(c => c.Name))
				{
					var itemVm = new ComponentItemViewModel(this, component);
					categoryVm.Components.Add(itemVm);
				}
				ComponentCategories.Add(categoryVm);
			}

			AvailablePersonas.Clear();
			var personasConfig = SettingsManager.Get<PersonasConfiguration>();
			foreach (var persona in PromptRegistry.BuiltinPersonas.Values)
				AvailablePersonas.Add(new PersonaItemViewModel(this, persona));
			foreach (var persona in personasConfig.Personas)
				AvailablePersonas.Add(new PersonaItemViewModel(this, persona));

			if (PromptSettings.PersonaId.HasValue)
			{
				SelectedPersona = AvailablePersonas.FirstOrDefault(p => p.Persona.Id == PromptSettings.PersonaId.Value);
			}
			else
			{
				SelectedPersona = null;
			}
		}

		public void UpdateSelectedComponents()
		{
			var selectedIds = new List<Guid>();
			foreach (var category in ComponentCategories)
			{
				foreach (var component in category.Components)
				{
					if (component.IsSelected)
						selectedIds.Add(component.Component.Id);
				}
			}
			PromptSettings.PromptComponents = selectedIds;
		}
	}
}