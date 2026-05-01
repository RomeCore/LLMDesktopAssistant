using LLMDesktopAssistant.Agents;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.Agents
{
	[ViewModelFor(typeof(AgentExecutionConditionsSettingsView))]
	public class AgentExecutionConditionsSettingsViewModel : ViewModelBase
	{
		public AgentExecutionConditionsSettings ExecutionConditionsSettings { get; }

		public AgentExecutionConditionsSettingsViewModel(AgentExecutionConditionsSettings settings)
		{
			ExecutionConditionsSettings = settings;
		}
	}
}
