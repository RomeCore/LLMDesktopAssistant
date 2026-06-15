using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Utils;
using LLMDesktopAssistant.LLM.Settings;

namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// Describes an agent tool settings.
	/// Contains the list of tools that can be used by the agent.
	/// </summary>
	public class AgentToolSettings : AgentSettingsCategoryBase
	{
		private bool _enableTools = true;
		/// <summary>
		/// Whether to use tools in the chat.
		/// </summary>
		public bool EnableTools
		{
			get => _enableTools;
			set => SetProperty(ref _enableTools, value);
		}

		private bool _enablePolicyOverride = false;
		/// <summary>
		/// Whether to override the approval policy for tools.
		/// When true, the <see cref="AutoApproveBehaviours"/> and <see cref="DisallowedBehaviours"/> 
		/// will be used instead of policy specified in <see cref="ChatToolsSettings"/>.
		/// </summary>
		public bool EnablePolicyOverride
		{
			get => _enablePolicyOverride;
			set => SetProperty(ref _enablePolicyOverride, value);
		}

		private ToolBehaviour _autoApproveBehaviours = ToolBehaviour.None;
		/// <summary>
		/// The behaviour of tools that will be automatically approved.
		/// </summary>
		public ToolBehaviour AutoApproveBehaviours
		{
			get => _autoApproveBehaviours;
			set => SetProperty(ref _autoApproveBehaviours, value);
		}

		private ToolBehaviour _disallowedBehaviours = ToolBehaviour.None;
		/// <summary>
		/// The behaviour of tools that will be disallowed.
		/// </summary>
		public ToolBehaviour DisallowedBehaviours
		{
			get => _disallowedBehaviours;
			set => SetProperty(ref _disallowedBehaviours, value);
		}

		private readonly RangeObservableCollection<ToolChange> _toolChanges = [];
		/// <summary>
		/// Gets or sets the tool changes.
		/// </summary>
		public ICollection<ToolChange> ToolChanges
		{
			get => _toolChanges;
			set => _toolChanges.Reset(value);
		}
	}
}