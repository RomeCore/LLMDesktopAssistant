namespace LLMDesktopAssistant.LLM.MVVM.Additional
{
	[ViewModelFor(typeof(TestAdditionalView))]
	public class TestAdditionalViewModel : AdditionalMessageViewModel
	{
		private string _greeting = "Hello, World!";
		public string Greeting
		{
			get => _greeting;
			set => SetProperty(ref _greeting, value);
		}
	}
}