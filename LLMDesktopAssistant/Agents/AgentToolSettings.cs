using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// Describes an agent tool settings.
	/// Contains the list of tools that can be used by the agent.
	/// </summary>
	public class AgentToolSettings : NotifyPropertyChanged
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

		private ToolDangerLevel _autoApproveLevel = ToolDangerLevel.Default;
		/// <summary>
		/// The maximum danger level of tools that can be automatically approved.
		/// </summary>
		public ToolDangerLevel AutoApproveLevel
		{
			get => _autoApproveLevel;
			set => SetProperty(ref _autoApproveLevel, value);
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