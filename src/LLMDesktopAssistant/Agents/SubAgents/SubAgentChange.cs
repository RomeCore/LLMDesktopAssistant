using LLMDesktopAssistant.Addons;

namespace LLMDesktopAssistant.Agents.SubAgents
{
	/// <summary>
	/// Represents a change applied to a sub-agent compared to the available sub-agent definitions.
	/// </summary>
	public class SubAgentChange : AddonChangeBase
	{
		private string? _model;
		/// <summary>
		/// The model override for the sub-agent. Null indicates that the setting has not been changed yet.
		/// </summary>
		public string? Model
		{
			get => _model;
			set => SetProperty(ref _model, value);
		}
	}
}
