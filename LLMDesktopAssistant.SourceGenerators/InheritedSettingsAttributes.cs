namespace LLMDesktopAssistant.SourceGenerators
{
	/// <summary>
	/// Marks a settings category class as a participant in the inherited settings system.
	/// The route is the name of the property on the parent settings object that points to this category.
	/// Example: for <c>AgentPromptSettings</c> used as <c>ChatAgentDescriptor.Prompts</c>, the route is <c>nameof(ChatAgentDescriptor.Prompts)</c>.
	/// </summary>
	[System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	[System.Diagnostics.Conditional("DASSGEN_ATTRIBUTES")]
	public sealed class SettingsRouteAttribute : System.Attribute
	{
		/// <summary>
		/// The name of the property on the parent settings object that points to this category.
		/// </summary>
		public string Route { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SettingsRouteAttribute"/> class.
		/// </summary>
		/// <param name="route">The name of the property on the parent settings object that points to this category.</param>
		public SettingsRouteAttribute(string route)
		{
			Route = route;
		}
	}

	/// <summary>
	/// Marks a property of an agent-level settings category (a category of <see cref="global::LLMDesktopAssistant.Agents.ChatAgentDescriptor"/>)
	/// as inheritable. The source generator emits an inheritance level property, an effective value getter
	/// and a write router for the marked property.
	/// Resolution levels: <c>Agent</c> (local), <c>Profile</c> (chat's <c>InheritedAgentSettings</c>), <c>Application</c> (app's <c>InheritedChatSettings.InheritedAgentSettings</c>).
	/// </summary>
	[System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
	[System.Diagnostics.Conditional("DASSGEN_ATTRIBUTES")]
	public sealed class InheritedChatAgentSettingAttribute : System.Attribute
	{
	}

	/// <summary>
	/// Marks a property of a chat-level settings category (a category of <see cref="global::LLMDesktopAssistant.LLM.Settings.ChatSettings"/>)
	/// as inheritable. The source generator emits an inheritance level property, an effective value getter
	/// and a write router for the marked property.
	/// Resolution levels: <c>Profile</c> (local chat value), <c>Application</c> (app's <c>InheritedChatSettings</c>).
	/// </summary>
	[System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
	[System.Diagnostics.Conditional("DASSGEN_ATTRIBUTES")]
	public sealed class InheritedChatSettingAttribute : System.Attribute
	{
	}
}
