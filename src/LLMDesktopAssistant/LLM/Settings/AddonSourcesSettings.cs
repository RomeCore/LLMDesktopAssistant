using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Settings
{
	public class AddonSourcesSettings : NotifyPropertyChanged
	{
		private readonly RangeObservableCollection<string> _additionalDirectories = [];
		/// <summary>
		/// Gets or sets the additional addon directories for specific type.
		/// </summary>
		public RangeObservableCollection<string> AdditionalDirectories
		{
			get => _additionalDirectories;
			set => _additionalDirectories.Reset(value);
		}

		private readonly RangeObservableCollection<string> _additionalFiles = [];
		/// <summary>
		/// Gets or sets the additional addon files.
		/// </summary>
		public RangeObservableCollection<string> AdditionalFiles
		{
			get => _additionalFiles;
			set => _additionalFiles.Reset(value);
		}
	}
}
