using LLMDesktopAssistant.SourceGenerators;

namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// Describes an agent execution conditions settings.
	/// The mention group is resolved through its effective (inherited) scope, selected via
	/// the inheritance level combo box in the view.
	/// </summary>
	[SettingsRoute(nameof(ChatAgentDescriptor.ExecutionConditions))]
	public partial class AgentExecutionConditionsSettings : AgentSettingsCategoryBase
	{
		private MentionSettings _mention = new();
		/// <summary>
		/// Gets or sets the mention group of the agent: whether the agent can be mentioned
		/// by others and can mention other agents.
		/// </summary>
		[InheritedChatAgentSetting]
		public MentionSettings Mention
		{
			get => _mention;
			set => SetProperty(ref _mention, value);
		}
	}
}
