using LLMDesktopAssistant.LLM.Domain;
using System;
using System.Collections.Generic;
using System.Text;

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