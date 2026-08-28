using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Services.Prompting;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Prompting;
using LLMDesktopAssistant.Prompting.Management;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.Agents;

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
	private readonly PromptBehaviourSliderValue _sliderValue;

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
		PromptBehaviourSliderValue sliderValue,
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

/// <summary>
/// ViewModel for the agent prompt settings: reusable prompt slot elements (system prompt,
/// persona, specialization) and prompt components shown as cards with diagnostics,
/// parameters and metadata, plus behavior sliders and the system prompt preview.
/// </summary>
[ViewModelFor(typeof(AgentPromptSettingsView))]
public class AgentPromptSettingsViewModel : ViewModelBase
{
	private readonly ChatSettings _chatSettings;
	private readonly IChatPromptBuilder _promptBuilder;
	private readonly ChatAgentDescriptor _agent;
	private readonly IPromptSlotElementManager _slotElementManager;
	private readonly IPromptComponentManager _componentManager;
	private readonly IPromptBehaviourSliderManager _behaviourSliderManager;

	/// <summary>
	/// Gets the underlying agent prompt settings.
	/// </summary>
	public AgentPromptSettings PromptSettings { get; }

	/// <summary>
	/// Gets the effective system prompt group resolved by the current inheritance level.
	/// </summary>
	public SystemPromptSettings EffectiveSystemPrompt => PromptSettings.GetEffectiveSystemPrompt(_chatSettings);

	/// <summary>
	/// Gets the effective persona group resolved by the current inheritance level.
	/// </summary>
	public PersonaSettings EffectivePersona => PromptSettings.GetEffectivePersona(_chatSettings);

	/// <summary>
	/// Gets the effective specialization group resolved by the current inheritance level.
	/// </summary>
	public SpecializationSettings EffectiveSpecialization => PromptSettings.GetEffectiveSpecialization(_chatSettings);

	/// <summary>
	/// Gets the effective prompt component selections resolved by the current inheritance level.
	/// </summary>
	public RangeObservableCollection<PromptPartKeyedSelection<Guid>> EffectivePromptComponents => PromptSettings.GetEffectivePromptComponents(_chatSettings);

	/// <summary>
	/// Gets the effective behavior slider values resolved by the current inheritance level.
	/// </summary>
	public RangeObservableCollection<PromptBehaviourSliderValue> EffectiveSliderValues => PromptSettings.GetEffectiveSliderValues(_chatSettings);

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

	private InheritanceLevelItem _selectedSliderValuesInheritance;
	/// <summary>
	/// Gets or sets the inheritance level for the behavior slider values.
	/// </summary>
	public InheritanceLevelItem SelectedSliderValuesInheritance
	{
		get => _selectedSliderValuesInheritance;
		set
		{
			if (SetProperty(ref _selectedSliderValuesInheritance, value) && value != null)
				PromptSettings.SliderValuesInheritance = value.Value;
		}
	}

	private string _searchText = string.Empty;
	/// <summary>
	/// Gets or sets the search text filtering the cards by name, description and category.
	/// </summary>
	public string SearchText
	{
		get => _searchText;
		set
		{
			if (SetProperty(ref _searchText, value))
				ApplyFilter();
		}
	}

	/// <summary>
	/// Full (unfiltered) card lists per group.
	/// </summary>
	private readonly List<PromptPartCardViewModel> _allSystemCards = [];
	private readonly List<PromptPartCardViewModel> _allPersonaCards = [];
	private readonly List<PromptPartCardViewModel> _allSpecializationCards = [];
	private readonly List<PromptPartCardViewModel> _allComponentCards = [];
	private readonly List<PromptPartCardViewModel> _allCards = [];

	/// <summary>
	/// Filtered card lists for display.
	/// </summary>
	public ObservableCollection<PromptPartCardViewModel> SystemElementCards { get; } = [];
	public ObservableCollection<PromptPartCardViewModel> PersonaElementCards { get; } = [];
	public ObservableCollection<PromptPartCardViewModel> SpecializationElementCards { get; } = [];
	public ObservableCollection<PromptPartCardViewModel> ComponentCards { get; } = [];

	/// <summary>
	/// Gets a value indicating whether the system prompt section has any visible cards.
	/// </summary>
	public bool IsSystemSectionVisible => SystemElementCards.Count > 0;

	/// <summary>
	/// Gets a value indicating whether the persona section has any visible cards.
	/// </summary>
	public bool IsPersonaSectionVisible => PersonaElementCards.Count > 0;

	/// <summary>
	/// Gets a value indicating whether the specialization section has any visible cards.
	/// </summary>
	public bool IsSpecializationSectionVisible => SpecializationElementCards.Count > 0;

	/// <summary>
	/// Gets a value indicating whether the components section has any visible cards.
	/// </summary>
	public bool IsComponentsSectionVisible => ComponentCards.Count > 0;

	/// <summary>
	/// Gets a value indicating whether any card is visible (for the empty state).
	/// </summary>
	public bool IsAnyCardVisible => IsSystemSectionVisible || IsPersonaSectionVisible || IsSpecializationSectionVisible || IsComponentsSectionVisible;

	/// <summary>
	/// Collection of behavior slider ViewModels for the UI.
	/// </summary>
	public ObservableCollection<BehaviorSliderItemViewModel> SliderItems { get; } = [];

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
		IChatPromptBuilder promptBuilder,
		ChatAgentDescriptor agent,
		IPromptComponentManager componentManager,
		IPromptSlotElementManager slotElementManager,
		IPromptBehaviourSliderManager behaviourSliderManager)
	{
		PromptSettings = settings;
		_chatSettings = chatSettings;
		_promptBuilder = promptBuilder;
		_agent = agent;

		_componentManager = componentManager;
		_slotElementManager = slotElementManager;
		_behaviourSliderManager = behaviourSliderManager;

		_selectedSystemPromptInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == settings.SystemPromptInheritance);
		_selectedComponentsInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == settings.PromptComponentsInheritance);
		_selectedPersonaInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == settings.PersonaInheritance);
		_selectedSpecializationInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == settings.SpecializationInheritance);
		_selectedSliderValuesInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == settings.SliderValuesInheritance);

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
				break;

			case nameof(AgentPromptSettings.PersonaInheritance):
				_selectedPersonaInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == PromptSettings.PersonaInheritance);
				RaisePropertyChanged(nameof(SelectedPersonaInheritance));
				RaisePropertyChanged(nameof(EffectivePersona));
				break;

			case nameof(AgentPromptSettings.SpecializationInheritance):
				_selectedSpecializationInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == PromptSettings.SpecializationInheritance);
				RaisePropertyChanged(nameof(SelectedSpecializationInheritance));
				RaisePropertyChanged(nameof(EffectiveSpecialization));
				break;

			case nameof(AgentPromptSettings.SliderValuesInheritance):
				_selectedSliderValuesInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == PromptSettings.SliderValuesInheritance);
				RaisePropertyChanged(nameof(SelectedSliderValuesInheritance));
				RaisePropertyChanged(nameof(EffectiveSliderValues));
				break;
		}

		SubscribeEffectiveObjects();
		Refresh();
		RegeneratePreview();
	}

	private SystemPromptSettings? _subscribedSystemPrompt;
	private PersonaSettings? _subscribedPersona;
	private SpecializationSettings? _subscribedSpecialization;
	private RangeObservableCollection<PromptPartKeyedSelection<Guid>>? _subscribedComponents;
	private RangeObservableCollection<PromptBehaviourSliderValue>? _subscribedSliderValues;
	private readonly List<PromptBehaviourSliderValue> _subscribedSliderValueItems = [];

	/// <summary>
	/// Subscribes to the currently effective groups and collections so that edits
	/// regenerate the preview and selection states live.
	/// </summary>
	private void SubscribeEffectiveObjects()
	{
		var systemPrompt = EffectiveSystemPrompt;
		if (!ReferenceEquals(_subscribedSystemPrompt, systemPrompt))
		{
			if (_subscribedSystemPrompt != null)
				_subscribedSystemPrompt.PropertyChanged -= OnEffectiveGroupChanged;
			_subscribedSystemPrompt = systemPrompt;
			systemPrompt.PropertyChanged += OnEffectiveGroupChanged;
		}

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

		var components = EffectivePromptComponents;
		if (!ReferenceEquals(_subscribedComponents, components))
		{
			if (_subscribedComponents != null)
				_subscribedComponents.CollectionChanged -= EffectiveComponents_CollectionChanged;
			_subscribedComponents = components;
			components.CollectionChanged += EffectiveComponents_CollectionChanged;
		}

		var sliderValues = EffectiveSliderValues;
		if (!ReferenceEquals(_subscribedSliderValues, sliderValues))
		{
			if (_subscribedSliderValues != null)
				_subscribedSliderValues.CollectionChanged -= EffectiveSliderValues_CollectionChanged;
			_subscribedSliderValues = sliderValues;
			sliderValues.CollectionChanged += EffectiveSliderValues_CollectionChanged;
		}
	}

	private void OnEffectiveGroupChanged(object? sender, PropertyChangedEventArgs e)
	{
		RefreshSelectionStates();
		RegeneratePreview();
	}

	private void EffectiveComponents_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		RefreshSelectionStates();
		RegeneratePreview();
	}

	private void EffectiveSliderValues_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		RegeneratePreview();
	}

	private void SubscribeSliderValue(PromptBehaviourSliderValue value)
	{
		if (_subscribedSliderValueItems.Contains(value))
			return;

		_subscribedSliderValueItems.Add(value);
		value.PropertyChanged += SliderValue_PropertyChanged;
	}

	private void SliderValue_PropertyChanged(object? sender, PropertyChangedEventArgs e)
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

	/// <summary>
	/// Rebuilds the card lists from the current managers and effective settings.
	/// </summary>
	public void Refresh()
	{
		// Dispose old cards
		_allCards.ForEach(c => c.Dispose());
		_allCards.Clear();
		_allSystemCards.Clear();
		_allPersonaCards.Clear();
		_allSpecializationCards.Clear();
		_allComponentCards.Clear();

		// --- Slot elements ---
		foreach (var element in _slotElementManager.GetAll())
		{
			PromptPartKeyedSelection<Guid>? selection = element.Kind switch
			{
				PromptSlotKind.System => EffectiveSystemPrompt,
				PromptSlotKind.Persona => EffectivePersona,
				PromptSlotKind.Specialization => EffectiveSpecialization,
				_ => null
			};
			var card = new PromptPartCardViewModel(this, element, element.Kind, selection, isRadio: true);
			_allCards.Add(card);
			switch (element.Kind)
			{
				case PromptSlotKind.System:
					_allSystemCards.Add(card);
					break;
				case PromptSlotKind.Persona:
					_allPersonaCards.Add(card);
					break;
				case PromptSlotKind.Specialization:
					_allSpecializationCards.Add(card);
					break;
			}
		}

		// --- Components ---
		var componentSelections = EffectivePromptComponents;
		foreach (var component in _componentManager.GetAll())
		{
			var selection = componentSelections.FirstOrDefault(s => s.Id == component.Guid);
			var card = new PromptPartCardViewModel(this, component, null, selection, isRadio: false);
			_allCards.Add(card);
			_allComponentCards.Add(card);
		}

		// --- Sliders ---
		SliderItems.Clear();
		var sliderValues = EffectiveSliderValues;
		foreach (var slider in _behaviourSliderManager.GetAll())
		{
			var sliderId = slider.Guid;
			// Find existing slider value or create new one with default (0)
			var existingValue = sliderValues.FirstOrDefault(sv => sv.Id == sliderId);
			if (existingValue == null)
			{
				existingValue = new PromptBehaviourSliderValue
				{
					Id = sliderId,
					Value = slider.DefaultValue
				};
				sliderValues.Add(existingValue);
			}

			SubscribeSliderValue(existingValue);

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

		ApplyFilter();
	}

	/// <summary>
	/// Gets a value indicating whether the given card is selected for the agent.
	/// </summary>
	public bool IsSelected(PromptPartCardViewModel card)
	{
		if (card.IsRadio)
		{
			var guid = card.Part.Guid;
			return card.Kind switch
			{
				PromptSlotKind.System => !EffectiveSystemPrompt.UseCustomSystemPrompt && EffectiveSystemPrompt.Id == guid,
				PromptSlotKind.Persona => !EffectivePersona.UseCustomPersona && EffectivePersona.Id == guid,
				PromptSlotKind.Specialization => !EffectiveSpecialization.UseCustomSpecialization && EffectiveSpecialization.Id == guid,
				_ => false
			};
		}

		return EffectivePromptComponents.Any(s => s.Id == card.Part.Guid);
	}

	/// <summary>
	/// Selects a radio card (slot element) for the agent and opens its parameters.
	/// </summary>
	public void SelectCard(PromptPartCardViewModel card)
	{
		if (!card.IsRadio)
			return;

		switch (card.Kind)
		{
			case PromptSlotKind.System:
				EffectiveSystemPrompt.UseCustomSystemPrompt = false;
				EffectiveSystemPrompt.Id = card.Part.Guid;
				break;
			case PromptSlotKind.Persona:
				EffectivePersona.UseCustomPersona = false;
				EffectivePersona.Id = card.Part.Guid;
				break;
			case PromptSlotKind.Specialization:
				EffectiveSpecialization.UseCustomSpecialization = false;
				EffectiveSpecialization.Id = card.Part.Guid;
				break;
		}

		card.EnsureParameters();
		card.IsParametersVisible = true;
	}

	/// <summary>
	/// Sets a checkbox card (component) selection for the agent.
	/// </summary>
	public void SetComponentSelected(PromptPartCardViewModel card, bool selected)
	{
		var collection = EffectivePromptComponents;
		var existing = collection.FirstOrDefault(s => s.Id == card.Part.Guid);

		if (selected && existing is null)
		{
			var selection = new PromptPartKeyedSelection<Guid> { Id = card.Part.Guid };
			collection.Add(selection);
			card.SetSelection(selection);
			card.EnsureParameters();
			card.IsParametersVisible = true;
		}
		else if (!selected && existing is not null)
		{
			collection.Remove(existing);
			card.SetSelection(null);
			card.IsParametersVisible = false;
		}
	}

	private void RefreshSelectionStates()
	{
		foreach (var card in _allCards)
			card.NotifySelectionChanged();
	}

	private void ApplyFilter()
	{
		var query = SearchText?.Trim() ?? string.Empty;

		ApplyFilterTo(SystemElementCards, _allSystemCards, query);
		ApplyFilterTo(PersonaElementCards, _allPersonaCards, query);
		ApplyFilterTo(SpecializationElementCards, _allSpecializationCards, query);
		ApplyFilterTo(ComponentCards, _allComponentCards, query);

		RaisePropertyChanged(nameof(IsSystemSectionVisible));
		RaisePropertyChanged(nameof(IsPersonaSectionVisible));
		RaisePropertyChanged(nameof(IsSpecializationSectionVisible));
		RaisePropertyChanged(nameof(IsComponentsSectionVisible));
		RaisePropertyChanged(nameof(IsAnyCardVisible));
	}

	private static void ApplyFilterTo(ObservableCollection<PromptPartCardViewModel> target, List<PromptPartCardViewModel> source, string query)
	{
		target.Clear();
		foreach (var card in source)
		{
			if (query.Length == 0 ||
				card.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
				card.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
				card.Category.Contains(query, StringComparison.OrdinalIgnoreCase))
			{
				target.Add(card);
			}
		}
	}

	/// <inheritdoc/>
	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);

		if (disposing)
		{
			PromptSettings.PropertyChanged -= PromptSettings_PropertyChanged;

			if (_subscribedSystemPrompt is not null)
				_subscribedSystemPrompt.PropertyChanged -= OnEffectiveGroupChanged;
			if (_subscribedPersona is not null)
				_subscribedPersona.PropertyChanged -= OnEffectiveGroupChanged;
			if (_subscribedSpecialization is not null)
				_subscribedSpecialization.PropertyChanged -= OnEffectiveGroupChanged;
			if (_subscribedComponents is not null)
				_subscribedComponents.CollectionChanged -= EffectiveComponents_CollectionChanged;
			if (_subscribedSliderValues is not null)
				_subscribedSliderValues.CollectionChanged -= EffectiveSliderValues_CollectionChanged;

			foreach (var value in _subscribedSliderValueItems)
				value.PropertyChanged -= SliderValue_PropertyChanged;
			_subscribedSliderValueItems.Clear();

			_allCards.ForEach(c => c.Dispose());
			_allCards.Clear();
		}
	}
}
