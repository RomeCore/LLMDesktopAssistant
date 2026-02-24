using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.Modules;
using RCLargeLanguageModels;

namespace LLMDesktopAssistant.LLM
{
	public interface ILLMProvider : IDynamicModule
	{
		public LLModel GetLLM();
	}
}