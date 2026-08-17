using LLMDesktopAssistant.Tools.Specifiers;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Tools
{
	/// <summary>
	/// Represents a change to a tool, including enabled state and confirmation requirements.
	/// </summary>
	public class ToolChange : NotifyPropertyChanged
	{
		/// <summary>
		/// The name of the tool being changed.
		/// </summary>
		public required string ToolName { get; init; }

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

		private ToolPolicyMask? _policyMask;
		/// <summary>
		/// Gets or sets the individual policy mask for the tool.
		/// </summary>
		public ToolPolicyMask? PolicyMask
		{
			get => _policyMask;
			set => SetProperty(ref _policyMask, value);
		}

		private SpecifierAggregationMode? _specifierAggregationMode;
		/// <summary>
		/// The specifier aggregation mode of the tool.
		/// </summary>
		public SpecifierAggregationMode? SpecifierAggregationMode
		{
			get => _specifierAggregationMode;
			set => SetProperty(ref _specifierAggregationMode, value);
		}

		private SpecifierBehaviourUnionMode? _specifierUnionMode;
		/// <summary>
		/// Gets or sets the specifier behaviour union mode for the tool.
		/// Null indicates that the default mode (<see cref="SpecifierBehaviourUnionMode.CombineSoft"/>) is used.
		/// </summary>
		public SpecifierBehaviourUnionMode? SpecifierUnionMode
		{
			get => _specifierUnionMode;
			set => SetProperty(ref _specifierUnionMode, value);
		}

		private readonly RangeObservableCollection<ToolSpecifierRule> _specifiers = [];
		/// <summary>
		/// Gets the list of specifier rules for the tool.
		/// </summary>
		public RangeObservableCollection<ToolSpecifierRule> Specifiers
		{
			get => _specifiers;
			set => _specifiers.Reset(value);
		}
	}
}