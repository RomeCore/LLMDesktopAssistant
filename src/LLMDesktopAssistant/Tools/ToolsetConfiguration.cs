using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;
using static AvaloniaEdit.Document.TextDocumentWeakEventManager;

namespace LLMDesktopAssistant.Tools
{
	/// <summary>
	/// A named toolset configuration that defines per-tool changes (enabled state and approval level).
	/// Instances are stored by <see cref="SettingsManager"/> and can be referenced by agents through
	/// <see cref="SettingsReference{ToolsetConfiguration}"/>.
	/// </summary>
	[SettingsObject("toolset")]
	public class ToolsetConfiguration : AddonSetConfigurationBase<ToolChange>
	{
		private ToolApprovalLevel _defaultApprovalLevel = ToolApprovalLevel.PolicyBased;
		/// <summary>
		/// Gets or sets the default approval level for unchanged tools.
		/// </summary>
		public ToolApprovalLevel DefaultApprovalLevel
		{
			get => _defaultApprovalLevel;
			set => SetProperty(ref _defaultApprovalLevel, value);
		}
	}
}
