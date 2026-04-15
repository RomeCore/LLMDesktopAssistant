using LLMDesktopAssistant.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.Core.Scripting
{
	[Service]
	public class MarkdownExecutionService
	{
		public bool IsExecutionAvaliable(string language)
		{
			return language switch
			{
				"python" or "py" => true,
				_ => false
			};
		}
	}
}