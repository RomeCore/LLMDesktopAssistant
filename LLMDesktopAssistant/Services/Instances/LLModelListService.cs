using RCLargeLanguageModels;
using RCLargeLanguageModels.Clients;
using RCLargeLanguageModels.Clients.Deepseek;
using RCLargeLanguageModels.Clients.Ollama;
using RCLargeLanguageModels.Clients.OpenAI;
using RCLargeLanguageModels.Clients.OpenRouter;
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

namespace LLMDesktopAssistant.Services.Instances
{
	[Service]
	public class LLModelListService
	{
		private class ExtendedDeepSeekClient : DeepSeekClient
		{
			public ExtendedDeepSeekClient(string endpointUri, ITokenAccessor tokenAccessor, HttpClient? http = null) : base(endpointUri, tokenAccessor, http)
			{
			}

			protected override Task<LLModelDescriptor[]> ListModelsOverrideAsync(CancellationToken cancellationToken = default)
			{
				var models = base.ListModelsOverrideAsync(cancellationToken).Result;

				return Task.FromResult(models.Append(
					new LLModelDescriptor(this, "deepseek-chat", "DeepSeek Chat")).Append(
					new LLModelDescriptor(this, "deepseek-reasoner", "DeepSeek Reasoner")).ToArray());
			}
		}

		static readonly ExtendedDeepSeekClient deepseek = new("https://api.deepseek.com/beta", new EnvironmentTokenAccessor("DEEPSEEK_API_KEY"));
		static readonly OpenRouterClient openrouter = new(new EnvironmentTokenAccessor("OPENROUTER_API_KEY"));
		static readonly OllamaClient ollama = new();

		public LLMClientRegistry Registry { get; private set; } = null!;

		public LLModelListService()
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
