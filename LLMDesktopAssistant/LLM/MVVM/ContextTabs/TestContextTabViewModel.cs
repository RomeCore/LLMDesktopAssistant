using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.LLM.MVVM.ContextTabs
{
	[ViewModelFor(typeof(TestContextTabView))]
	public class TestContextTabViewModel : ChatContextTabViewModel
	{
		private string _greeting = "Hello Context Tab!";
		public string Greeting
		{
			get => _greeting;
			set => SetProperty(ref _greeting, value);
		}

		public TestContextTabViewModel()
		{
			Title = "Test";
		}
	}
}