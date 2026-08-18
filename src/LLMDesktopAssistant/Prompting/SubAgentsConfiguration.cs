using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Prompting
{
	/// <summary>
	/// Represents the configuration of sub-agent prompts.
	/// </summary>
	public class SubAgentsConfiguration : SettingsObject
	{
		private readonly RangeObservableCollection<SubAgentPrompt> _subAgents = [];
		/// <summary>
		/// Gets or sets the list of sub-agents.
		/// </summary>
		public ICollection<SubAgentPrompt> SubAgents
		{
			get => _subAgents;
			set => _subAgents.Reset(value);
		}
	}
}
