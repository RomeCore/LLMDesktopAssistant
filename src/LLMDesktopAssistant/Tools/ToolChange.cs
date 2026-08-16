namespace LLMDesktopAssistant.Tools
{
	/// <summary>
	/// Represents a change to a tool, including enabled state and confirmation requirements.
	/// </summary>
	public class ToolChange : NotifyPropertyChanged
	{
		private string _toolName = string.Empty;
		/// <summary>
		/// The name of the tool being changed.
		/// </summary>
		public string ToolName
		{
			get => _toolName;
			set => SetProperty(ref _toolName, value);
		}

		private bool? _enabled;
		/// <summary>
		/// Whether the tool is enabled or not. Null indicates that the setting has not been changed yet.
		/// </summary>
		public bool? Enabled
		{
			get => _enabled;
			set => SetProperty(ref _enabled, value);
		}

		private ToolApprovalLevel? _approvalLevel;
		/// <summary>
		/// Gets or sets a value indicating the approval level of a tool.
		/// Null indicates that the setting has not been changed yet.
		/// </summary>
		public ToolApprovalLevel? ApprovalLevel
		{
			get => _approvalLevel;
			set => SetProperty(ref _approvalLevel, value);
		}

		private ToolIndividualPolicyMask? _policyMask;
		/// <summary>
		/// Gets or sets the individual policy mask for the tool.
		/// </summary>
		public ToolIndividualPolicyMask? PolicyMask
		{
			get => _policyMask;
			set => SetProperty(ref _policyMask, value);
		}
	}
}