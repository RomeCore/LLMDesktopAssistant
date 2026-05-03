using LLMDesktopAssistant.Settings;
using RCLargeLanguageModels.Clients;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Settings for conversation auto-summarization.
	/// </summary>
	public class ChatSummarizationSettings : NotifyPropertyChanged
	{
		private bool _summarizationEnabled = true;
		/// <summary>
		/// Whether auto-summarization is enabled.
		/// Auto summarization triggers when total usage tokens exceeds a certain threshold (<see cref="SummarizationTriggerTokens"/>).
		/// </summary>
		public bool AutoSummarizationEnabled
		{
			get => _summarizationEnabled;
			set => SetProperty(ref _summarizationEnabled, value);
		}

		private int _summarizationTriggerTokens = 102400; // 100k tokens by default
		/// <summary>
		/// The number of tokens that must be reached before auto-summarization is triggered.
		/// </summary>
		public int SummarizationTriggerTokens
		{
			get => _summarizationTriggerTokens;
			set => SetProperty(ref _summarizationTriggerTokens, value);
		}

		private int _ignoreLastRounds = 3;
		/// <summary>
		/// The number of turns that will be ignored when auto-summarizing.
		/// </summary>
		public int IgnoreLastRounds
		{
			get => _ignoreLastRounds;
			set => SetProperty(ref _ignoreLastRounds, value);
		}

		private LLModelDescriptorTracked _summarizerModel = LLModelDescriptorTracked.Empty;
		/// <summary>
		/// The model to use for summarizing the conversation for compacting.
		/// </summary>
		public LLModelDescriptorTracked SummarizerModel
		{
			get => _summarizerModel;
			set => SetProperty(ref _summarizerModel, value);
		}
	}
}