using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Settings for tools and function calling.
	/// </summary>
	public class ChatToolSettings : NotifyPropertyChanged
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