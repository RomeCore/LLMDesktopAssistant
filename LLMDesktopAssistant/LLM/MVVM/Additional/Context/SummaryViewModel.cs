using LiteDB;
using LLMDesktopAssistant.LLM.Domain;

namespace LLMDesktopAssistant.LLM.MVVM.Additional.Context
{
	/// <summary>
	/// ViewModel for summarizing messages.
	/// </summary>
	[ViewModelFor(typeof(SummaryView))]
	public class SummaryViewModel : AdditionalMessageViewModel
	{
		private string _summary = string.Empty;
		public string Summary
		{
			get => _summary;
			set => SetProperty(ref _summary, value);
		}

		private bool _completed = true;
		[BsonIgnore]
		public bool Completed
		{
			get => _completed;
			set => SetProperty(ref _completed, value);
		}

		public override int Order => 50;
	}
}
