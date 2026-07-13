using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Prompting;
using LLMDesktopAssistant.Settings;
using LLTSharp;
using LLTSharp.Metadata;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.Agents
{
	public class ComponentItemViewModel : NotifyPropertyChanged
	{
		private readonly AgentPromptSettingsViewModel _parent;
		public PromptComponent Component { get; }

		public ComponentItemViewModel(AgentPromptSettingsViewModel parent, PromptComponent component)
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

	public class ComponentCategoryViewModel : NotifyPropertyChanged
	{
		public string CategoryName { get; }
		public ObservableCollection<ComponentItemViewModel> Components { get; } = new();

		public ComponentCategoryViewModel(string categoryName)
		{
			CategoryName = categoryName;
		}
	}

	public class PersonaItemViewModel : NotifyPropertyChanged
	{
		private readonly AgentPromptSettingsViewModel _parent;
		public Persona Persona { get; }

		public PersonaItemViewModel(AgentPromptSettingsViewModel parent, Persona persona)
		{
			_parent = parent;
			Persona = persona;
		}
	}

	public class SpecializationItemViewModel : NotifyPropertyChanged
	{
		private readonly AgentPromptSettingsViewModel _parent;
		public Specialization Specialization { get; }

		public SpecializationItemViewModel(AgentPromptSettingsViewModel parent, Specialization specialization)
		{
			_parent = parent;
			Specialization = specialization;
		}
	}

	public class BehaviorSliderHintViewModel : NotifyPropertyChanged
	{
		public string? Label { get; set; }

		public int Column { get; set; }
	}

	/// <summary>
	/// ViewModel for a single behavior slider.
	/// Loads metadata from the slider's .llt template definition.
	/// </summary>
	public class BehaviorSliderItemViewModel : NotifyPropertyChanged
	{
		private readonly AgentPromptSettingsViewModel _parent;
		private readonly BehaviorSliderValue _sliderValue;

		/// <summary>
		/// The GUID of the slider definition.
		/// </summary>
		public Guid SliderId { get; }

		/// <summary>
		/// Display name of the slider (from .llt metadata "title").
		/// </summary>
		public string DisplayName { get; }

		/// <summary>
		/// Minimum value of the slider (from .llt metadata "sliderMin").
		/// </summary>
		public int MinValue { get; }

		/// <summary>
		/// Maximum value of the slider (from .llt metadata "sliderMax").
		/// </summary>
		public int MaxValue { get; }

		/// <summary>
		/// Hints/labels for each slider position (from .llt metadata "hints").
		/// Index 0 corresponds to MinValue, last index to MaxValue.
		/// null entries mean no label for that position.
		/// </summary>
		public BehaviorSliderHintViewModel[] Hints { get; }

		/// <summary>
		/// The number of positions on the slider. Range = MaxValue - MinValue + 1.
		/// </summary>
		public int Range => MaxValue - MinValue + 1;

		/// <summary>
		/// The current value of the slider.
		/// </summary>
		public int Value
		{
			get => _sliderValue.Value;
			set
			{
				if (_sliderValue.Value != value)
				{
					_sliderValue.Value = value;
					RaisePropertyChanged();
				}
			}
		}

		public BehaviorSliderItemViewModel(
			AgentPromptSettingsViewModel parent,
			BehaviorSliderValue sliderValue,
			Guid sliderId,
			string displayName,
			int minValue,
			int maxValue,
			BehaviorSliderHintViewModel[] hints)
		{
			_parent = parent;
			_sliderValue = sliderValue;
			SliderId = sliderId;
			DisplayName = displayName;
			MinValue = minValue;
			MaxValue = maxValue;
			Hints = hints;
		}
	}


	[ViewModelFor(typeof(AgentPromptSettingsView))]
	public class AgentPromptSettingsViewModel : ViewModelBase
	{
		public AgentPromptSettings PromptSettings { get; }
		public PromptRegistry PromptRegistry { get; }

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

		public ObservableCollection<SpecializationItemViewModel> AvailableSpecializations { get; } = new();
		private SpecializationItemViewModel? _selectedSpecialization;
		public SpecializationItemViewModel? SelectedSpecialization
		{
			get => _selectedSpecialization;
			set
			{
				if (SetProperty(ref _selectedSpecialization, value))
				{
					if (value != null)
						PromptSettings.SpecializationId = value.Specialization.Id;
					else
						PromptSettings.SpecializationId = null;
				}
			}
		}

		public ICommand ClearSpecializationCommand { get; }

		/// <summary>
		/// Collection of behavior slider ViewModels for the UI.
		/// </summary>
		public ObservableCollection<BehaviorSliderItemViewModel> SliderItems { get; } = new();

		public AgentPromptSettingsViewModel(AgentPromptSettings settings, IPromptRegistry promptRegistry)
		{
			PromptSettings = settings;
			PromptRegistry = promptRegistry as PromptRegistry ?? throw new InvalidOperationException("Prompt registry must be of type PromptRegistry.");

			ClearPersonaCommand = new RelayCommand(() => SelectedPersona = null);
			ClearSpecializationCommand = new RelayCommand(() => SelectedSpecialization = null);
			Refresh();
		}

		public void Refresh()
		{
			// --- Components ---
			var allComponents = new List<PromptComponent>();
			var componentsConfig = SettingsManager.Get<PromptComponentsConfiguration>();
			allComponents.AddRange(componentsConfig.Components);
			allComponents.AddRange(PromptRegistry.BuiltinComponents.Values);

			var grouped = allComponents.GroupBy(c => string.IsNullOrEmpty(c.Category)
				? LocalizationManager.LocalizeStatic("prompt_category_uncategorized")
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

			// --- Personas ---
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

			// --- Specializations ---
			AvailableSpecializations.Clear();
			var specializationsConfig = SettingsManager.Get<SpecializationsConfiguration>();
			foreach (var specialization in PromptRegistry.BuiltinSpecializations.Values)
				AvailableSpecializations.Add(new SpecializationItemViewModel(this, specialization));
			foreach (var specialization in specializationsConfig.Specializations)
				AvailableSpecializations.Add(new SpecializationItemViewModel(this, specialization));

			if (PromptSettings.SpecializationId.HasValue)
			{
				SelectedSpecialization = AvailableSpecializations.FirstOrDefault(s => s.Specialization.Id == PromptSettings.SpecializationId.Value);
			}
			else
			{
				SelectedSpecialization = null;
			}

			// --- Sliders ---
			foreach (var (sliderId, slider) in PromptRegistry.BuiltinSliders)
			{
				// Find existing slider value or create new one with default (0)
				var existingValue = PromptSettings.SliderValues.FirstOrDefault(sv => sv.SliderId == sliderId);
				if (existingValue == null)
				{
					existingValue = new BehaviorSliderValue
					{
						SliderId = sliderId,
						Value = slider.DefaultValue
					};
					PromptSettings.SliderValues.Add(existingValue);
				}

				var itemVm = new BehaviorSliderItemViewModel(
					this,
					existingValue,
					sliderId,
					slider.Name,
					slider.MinimumValue,
					slider.MaximumValue,
					slider.Titles.Values
						.Select((label, index) => new BehaviorSliderHintViewModel
						{
							Label = label,
							Column = index
						})
						.ToArray());

				SliderItems.Add(itemVm);
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
