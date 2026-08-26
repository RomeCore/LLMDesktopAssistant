using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Tools
{
	/// <summary>
	/// A named toolset configuration that defines per-tool changes (enabled state and approval level).
	/// Instances are stored by <see cref="SettingsManager"/> and can be referenced by agents through
	/// <see cref="SettingsReference{ToolsetConfiguration}"/>.
	/// </summary>
	[SettingsObject("toolset")]
	public class ToolsetConfiguration : SettingsObject
	{
		private bool _toolsEnabledByDefault = true;
		/// <summary>
		/// Gets or sets a value indicating whether unchanged tools are enabled by default.
		/// </summary>
		public bool ToolsEnabledByDefault
		{
			get => _toolsEnabledByDefault;
			set => SetProperty(ref _toolsEnabledByDefault, value);
		}

		private ToolApprovalLevel _defaultApprovalLevel = ToolApprovalLevel.PolicyBased;
		/// <summary>
		/// Gets or sets the default approval level for unchanged tools.
		/// </summary>
		public ToolApprovalLevel DefaultApprovalLevel
		{
			get => _defaultApprovalLevel;
			set => SetProperty(ref _defaultApprovalLevel, value);
		}

		private readonly RangeObservableCollection<ToolChange> _toolChanges = [];
		/// <summary>
		/// Gets or sets the tool changes that override default tool settings.
		/// </summary>
		public RangeObservableCollection<ToolChange> ToolChanges
		{
			get => _toolChanges;
			set => _toolChanges.Reset(value);
		}
	}
}
