using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Services.Prompting;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Prompting;
using LLMDesktopAssistant.Settings;

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
			_isSelected = parent.EffectivePromptComponents.Contains(component.Id);
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
		/// <summary>
		/// Gets the underlying agent prompt settings.
		/// </summary>
		public AgentPromptSettings PromptSettings { get; }

		/// <summary>
		/// Gets the prompt registry used to resolve components, personas and specializations.
		/// </summary>
		public PromptRegistry PromptRegistry { get; }

		private readonly ChatSettings _chatSettings;
		private readonly IChatPromptBuilder _promptBuilder;
		private readonly ChatAgentDescriptor _agent;

		/// <summary>
		/// Gets the effective system prompt resolved by the current inheritance level.
		/// </summary>
		public string? EffectiveSystemPrompt
		{
			get => PromptSettings.GetEffectiveSystemPrompt(_chatSettings);
			set
			{
				PromptSettings.SetEffectiveSystemPrompt(_chatSettings, value);
				RegeneratePreview();
			}
		}

		/// <summary>
		/// Gets the effective prompt components resolved by the current inheritance level.
		/// </summary>
		public ICollection<Guid> EffectivePromptComponents => PromptSettings.GetEffectivePromptComponents(_chatSettings);

		/// <summary>
		/// Gets the effective persona group resolved by the current inheritance level.
		/// </summary>
		public PersonaSettings EffectivePersona => PromptSettings.GetEffectivePersona(_chatSettings);

		/// <summary>
		/// Gets the effective specialization group resolved by the current inheritance level.
		/// </summary>
		public SpecializationSettings EffectiveSpecialization => PromptSettings.GetEffectiveSpecialization(_chatSettings);

		/// <summary>
		/// Gets the effective behavior sliders group resolved by the current inheritance level.
		/// </summary>
		public SliderValuesSettings EffectiveSliders => PromptSettings.GetEffectiveSliders(_chatSettings);

		private InheritanceLevelItem _selectedSystemPromptInheritance;
		/// <summary>
		/// Gets or sets the inheritance level for the system prompt group.
		/// </summary>
		public InheritanceLevelItem SelectedSystemPromptInheritance
		{
			get => _selectedSystemPromptInheritance;
			set
			{
				if (SetProperty(ref _selectedSystemPromptInheritance, value) && value != null)
					PromptSettings.SystemPromptInheritance = value.Value;
			}
		}

		private InheritanceLevelItem _selectedComponentsInheritance;
		/// <summary>
		/// Gets or sets the inheritance level for the prompt components group.
		/// </summary>
		public InheritanceLevelItem SelectedComponentsInheritance
		{
			get => _selectedComponentsInheritance;
			set
			{
				if (SetProperty(ref _selectedComponentsInheritance, value) && value != null)
					PromptSettings.PromptComponentsInheritance = value.Value;
			}
		}

		private InheritanceLevelItem _selectedPersonaInheritance;
		/// <summary>
		/// Gets or sets the inheritance level for the persona group.
		/// </summary>
		public InheritanceLevelItem SelectedPersonaInheritance
		{
			get => _selectedPersonaInheritance;
			set
			{
				if (SetProperty(ref _selectedPersonaInheritance, value) && value != null)
					PromptSettings.PersonaInheritance = value.Value;
			}
		}

		private InheritanceLevelItem _selectedSpecializationInheritance;
		/// <summary>
		/// Gets or sets the inheritance level for the specialization group.
		/// </summary>
		public InheritanceLevelItem SelectedSpecializationInheritance
		{
			get => _selectedSpecializationInheritance;
			set
			{
				if (SetProperty(ref _selectedSpecializationInheritance, value) && value != null)
					PromptSettings.SpecializationInheritance = value.Value;
			}
		}

		private InheritanceLevelItem _selectedSlidersInheritance;
		/// <summary>
		/// Gets or sets the inheritance level for the behavior sliders group.
		/// </summary>
		public InheritanceLevelItem SelectedSlidersInheritance
		{
			get => _selectedSlidersInheritance;
			set
			{
				if (SetProperty(ref _selectedSlidersInheritance, value) && value != null)
					PromptSettings.SlidersInheritance = value.Value;
			}
		}

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
					EffectivePersona.PersonaId = value?.Persona.Id;
					RegeneratePreview();
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
					EffectiveSpecialization.SpecializationId = value?.Specialization.Id;
					RegeneratePreview();
				}
			}
		}

		public ICommand ClearSpecializationCommand { get; }

		/// <summary>
		/// Collection of behavior slider ViewModels for the UI.
		/// </summary>
		public ObservableCollection<BehaviorSliderItemViewModel> SliderItems { get; } = new();

		private bool _isPreviewVisible;
		/// <summary>
		/// Whether the system prompt preview is visible.
		/// </summary>
		public bool IsPreviewVisible
		{
			get => _isPreviewVisible;
			set => SetProperty(ref _isPreviewVisible, value);
		}

		private string _systemPromptPreview = string.Empty;
		/// <summary>
		/// The fully rendered system prompt preview text.
		/// </summary>
		public string SystemPromptPreview
		{
			get => _systemPromptPreview;
			private set => SetProperty(ref _systemPromptPreview, value);
		}

		public ICommand TogglePreviewCommand { get; }

		public AgentPromptSettingsViewModel(
			AgentPromptSettings settings,
			ChatSettings chatSettings,
			IPromptRegistry promptRegistry,
			IChatPromptBuilder promptBuilder,
			ChatAgentDescriptor agent)
		{
			PromptSettings = settings;
			_chatSettings = chatSettings;
			PromptRegistry = promptRegistry as PromptRegistry ?? throw new InvalidOperationException("Prompt registry must be of type PromptRegistry.");
			_promptBuilder = promptBuilder;
			_agent = agent;

			_selectedSystemPromptInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == settings.SystemPromptInheritance);
			_selectedComponentsInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == settings.PromptComponentsInheritance);
			_selectedPersonaInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == settings.PersonaInheritance);
			_selectedSpecializationInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == settings.SpecializationInheritance);
			_selectedSlidersInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == settings.SlidersInheritance);

			ClearPersonaCommand = new RelayCommand(() => SelectedPersona = null);
			ClearSpecializationCommand = new RelayCommand(() => SelectedSpecialization = null);
			TogglePreviewCommand = new RelayCommand(() => IsPreviewVisible = !IsPreviewVisible);

			settings.PropertyChanged += PromptSettings_PropertyChanged;
			SubscribeEffectiveObjects();

			Refresh();
			RegeneratePreview();
		}

		private void PromptSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(AgentPromptSettings.SystemPromptInheritance):
					_selectedSystemPromptInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == PromptSettings.SystemPromptInheritance);
					RaisePropertyChanged(nameof(SelectedSystemPromptInheritance));
					RaisePropertyChanged(nameof(EffectiveSystemPrompt));
					break;

				case nameof(AgentPromptSettings.PromptComponentsInheritance):
					_selectedComponentsInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == PromptSettings.PromptComponentsInheritance);
					RaisePropertyChanged(nameof(SelectedComponentsInheritance));
					RaisePropertyChanged(nameof(EffectivePromptComponents));
					Refresh();
					break;

				case nameof(AgentPromptSettings.PersonaInheritance):
					_selectedPersonaInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == PromptSettings.PersonaInheritance);
					RaisePropertyChanged(nameof(SelectedPersonaInheritance));
					RaisePropertyChanged(nameof(EffectivePersona));
					SubscribeEffectiveObjects();
					Refresh();
					break;

				case nameof(AgentPromptSettings.SpecializationInheritance):
					_selectedSpecializationInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == PromptSettings.SpecializationInheritance);
					RaisePropertyChanged(nameof(SelectedSpecializationInheritance));
					RaisePropertyChanged(nameof(EffectiveSpecialization));
					SubscribeEffectiveObjects();
					Refresh();
					break;

				case nameof(AgentPromptSettings.SlidersInheritance):
					_selectedSlidersInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == PromptSettings.SlidersInheritance);
					RaisePropertyChanged(nameof(SelectedSlidersInheritance));
					RaisePropertyChanged(nameof(EffectiveSliders));
					Refresh();
					break;
			}

			RegeneratePreview();
		}

		private PersonaSettings? _subscribedPersona;
		private SpecializationSettings? _subscribedSpecialization;

		/// <summary>
		/// Subscribes to the currently effective persona and specialization objects so that
		/// edits to inherited groups regenerate the preview live.
		/// </summary>
		private void SubscribeEffectiveObjects()
		{
			var persona = EffectivePersona;
			if (!ReferenceEquals(_subscribedPersona, persona))
			{
				if (_subscribedPersona != null)
					_subscribedPersona.PropertyChanged -= OnEffectiveGroupChanged;
				_subscribedPersona = persona;
				persona.PropertyChanged += OnEffectiveGroupChanged;
			}

			var specialization = EffectiveSpecialization;
			if (!ReferenceEquals(_subscribedSpecialization, specialization))
			{
				if (_subscribedSpecialization != null)
					_subscribedSpecialization.PropertyChanged -= OnEffectiveGroupChanged;
				_subscribedSpecialization = specialization;
				specialization.PropertyChanged += OnEffectiveGroupChanged;
			}
		}

		private void OnEffectiveGroupChanged(object? sender, PropertyChangedEventArgs e)
		{
			RegeneratePreview();
		}

		/// <summary>
		/// Regenerates the system prompt preview using <see cref="IChatPromptBuilder.RenderSystemPrompt"/>.
		/// </summary>
		public void RegeneratePreview()
		{
			try
			{
				SystemPromptPreview = _promptBuilder.RenderSystemPrompt(_agent);
			}
			catch (Exception ex)
			{
				SystemPromptPreview = $"// Failed to render preview: {ex.Message}";
			}
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

			if (EffectivePersona.PersonaId.HasValue)
			{
				SelectedPersona = AvailablePersonas.FirstOrDefault(p => p.Persona.Id == EffectivePersona.PersonaId.Value);
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

			if (EffectiveSpecialization.SpecializationId.HasValue)
			{
				SelectedSpecialization = AvailableSpecializations.FirstOrDefault(s => s.Specialization.Id == EffectiveSpecialization.SpecializationId.Value);
			}
			else
			{
				SelectedSpecialization = null;
			}

			// --- Sliders ---
			SliderItems.Clear();
			var sliderValues = EffectiveSliders.Items;
			foreach (var (sliderId, slider) in PromptRegistry.BuiltinSliders)
			{
				// Find existing slider value or create new one with default (0)
				var existingValue = sliderValues.FirstOrDefault(sv => sv.SliderId == sliderId);
				if (existingValue == null)
				{
					existingValue = new BehaviorSliderValue
					{
						SliderId = sliderId,
						Value = slider.DefaultValue
					};
					sliderValues.Add(existingValue);
				}

				existingValue.PropertyChanged += (_, _) => RegeneratePreview();

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

			var effectiveComponents = EffectivePromptComponents;
			effectiveComponents.Clear();
			foreach (var id in selectedIds)
				effectiveComponents.Add(id);

			RegeneratePreview();
		}
	}
}
