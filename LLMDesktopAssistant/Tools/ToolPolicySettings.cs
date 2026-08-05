namespace LLMDesktopAssistant.Tools
{
	/// <summary>
	/// Represents the tool behaviour policy for an agent: the behaviours that are automatically
	/// approved and the behaviours that are disallowed.
	/// </summary>
	public class ToolPolicySettings : NotifyPropertyChanged
	{
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
	}
}
