using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.Modules;
using RCLargeLanguageModels;
using RCLargeLanguageModels.Clients.Deepseek;
using RCLargeLanguageModels.Security;

namespace LLMDesktopAssistant.LLM
{
	[DynamicModule("DeepSeekLLMProvider", typeof(ILLMProvider), IsDefault = true)]
	public class DeepSeekLLMProvider : ILLMProvider
	{
		private readonly LLModel _llm;

		public DeepSeekLLMProvider()
		{
			var client = new DeepSeekClient(new EnvironmentTokenAccessor("DEEPSEEK_API_KEY"));

			_llm = new LLModel(client, "deepseek-chat");
		}

		public LLModel GetLLM()
		{
			return _llm;
		}
	}
}