using LLMDesktopAssistant.SourceGenerators;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// Describes an agent prompt settings.
	/// Contains the system prompt, prompt components, the persona group, the specialization
	/// group and the behavior sliders group. All groups are resolved through their effective
	/// (inherited) scope, selected via the inheritance level combo boxes in the view.
	/// </summary>
	[SettingsRoute(nameof(ChatAgentDescriptor.Prompts))]
	public partial class AgentPromptSettings : AgentSettingsCategoryBase
	{
		private string? _systemPrompt;
		/// <summary>
		/// The system prompt to use for the agent.
		/// </summary>
		[InheritedChatAgentSetting]
		public string? SystemPrompt
		{
			get => _systemPrompt;
			set => SetProperty(ref _systemPrompt, value);
		}

		private readonly RangeObservableCollection<Guid> _promptComponents = [];
		/// <summary>
		/// The collection of prompt components IDs that should be appended to the system message in addition to the <see cref="SystemPrompt"/>.
		/// The identifiers leads to <see cref="Prompting.PromptRegistry.GetComponent(Guid)"/>
		/// </summary>
		[InheritedChatAgentSetting]
		public ICollection<Guid> PromptComponents
		{
			get => _promptComponents;
			set => _promptComponents.Reset(value);
		}

		private PersonaSettings _persona = new();
		/// <summary>
		/// Gets or sets the persona group of the agent: the nickname and the persona selection.
		/// </summary>
		[InheritedChatAgentSetting]
		public PersonaSettings Persona
		{
			get => _persona;
			set => SetProperty(ref _persona, value);
		}

		private SpecializationSettings _specialization = new();
		/// <summary>
		/// Gets or sets the specialization group of the agent.
		/// </summary>
		[InheritedChatAgentSetting]
		public SpecializationSettings Specialization
		{
			get => _specialization;
			set => SetProperty(ref _specialization, value);
		}

		private SliderValuesSettings _sliders = new();
		/// <summary>
		/// Gets or sets the behavior sliders group of the agent.
		/// </summary>
		[InheritedChatAgentSetting]
		public SliderValuesSettings Sliders
		{
			get => _sliders;
			set => SetProperty(ref _sliders, value);
		}
	}
}
