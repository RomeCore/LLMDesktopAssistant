using LLMDesktopAssistant.Agents.Memory;
using LLMDesktopAssistant.Settings;

namespace LLMDesktopAssistant.Agents
{
	public class MemoryBlockAttachment : NotifyPropertyChanged
	{
		private bool _enabled = true;
		/// <summary>
		/// Whether the memory block is enabled.
		/// </summary>
		public bool Enabled
		{
			get => _enabled;
			set => SetProperty(ref _enabled, value);
		}

		/// <summary>
		/// The memory block setting reference.
		/// </summary>
		public SettingsReference<MemoryBlock> Reference { get; init; } = new();
	}
}