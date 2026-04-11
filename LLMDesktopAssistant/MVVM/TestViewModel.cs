using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.Core.MVVM
{
	[ViewModelFor(typeof(TestView))]
	public class TestViewModel
	{
		public string Name { get; set; } = "John Doe";

		public int Age { get; set; } = 30;
	}
}