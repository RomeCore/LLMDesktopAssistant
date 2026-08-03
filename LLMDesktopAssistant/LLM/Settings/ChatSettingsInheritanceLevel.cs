using LLMDesktopAssistant.Agents;

namespace LLMDesktopAssistant.LLM.Settings
{
	public enum ChatSettingsInheritanceLevel
	{
		/// <summary>
		/// Setting uses application-level setting value.
		/// </summary>
		Application,

		/// <summary>
		/// Setting uses chat profile-level setting value.
		/// This is default value for <see cref="ChatSettings"/>'s values.
		/// </summary>
		Profile,

		/// <summary>
		/// Setting uses agent-level (highest) setting value.
		/// This is default value for <see cref="ChatAgentDescriptor"/>'s values.
		/// </summary>
		Agent
	}

	
}
