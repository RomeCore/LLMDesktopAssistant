using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using RCLargeLanguageModels.Clients;
using RCLargeLanguageModels.Clients.Deepseek;
using RCLargeLanguageModels.Clients.OpenAI;
using RCLargeLanguageModels.Security;

namespace LLMDesktopAssistant.Modules.Instances
{
	[Module]
	public class LLModelListModule : Module
	{
		private class OpenrouterClient : OpenAICompatibleClient
		{
			public OpenrouterClient(ITokenAccessor tokenAccessor, HttpClient? http = null) :
				base("https://openrouter.ai/api/v1", tokenAccessor, http)
			{
			}

			public override string Name => "openrouter";
			public override string DisplayName => "Openrouter";
		}

		static readonly DeepSeekClient deepseek = new(new EnvironmentTokenAccessor("DEEPSEEK_API_KEY"));
		static readonly OpenrouterClient openrouter = new(new EnvironmentTokenAccessor("OPENROUTER_API_KEY"));

		public LLMClientRegistry Registry { get; private set; } = null!;

		public override void Initialize()
		{
			base.Initialize();

			Registry = new LLMClientRegistry(new LLMClientRegistryProperties
			{
				IsAutoRefreshEnabled = true,
				RefreshingMaxWaitMs = 5000,
				RefreshingUpdateIntervalS = 30
			});
			Registry.Register(deepseek);
			Registry.Register(openrouter);
		}
	}
}
