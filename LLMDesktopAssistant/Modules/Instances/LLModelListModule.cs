using RCLargeLanguageModels;
using RCLargeLanguageModels.Clients;
using RCLargeLanguageModels.Clients.Deepseek;
using RCLargeLanguageModels.Clients.Ollama;
using RCLargeLanguageModels.Clients.OpenAI;
using RCLargeLanguageModels.Security;
using RCLargeLanguageModels.Tasks;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

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
		static readonly OllamaClient ollama = new();

		public LLMClientRegistry Registry { get; private set; } = null!;

		public LLModelListModule()
		{
			Registry = new LLMClientRegistry(new LLMClientRegistryProperties
			{
				IsAutoRefreshEnabled = false,
				RefreshingMaxWaitMs = 5000,
				RefreshingUpdateIntervalS = 30
			});

			Registry.Register(deepseek);
			Registry.Register(openrouter);
			Registry.Register(ollama);

			_ = Registry.RefreshModelsAsync();
		}
	}
}
