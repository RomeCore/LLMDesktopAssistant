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
