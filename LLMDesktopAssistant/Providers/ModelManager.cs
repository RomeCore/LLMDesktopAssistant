using System.Runtime.CompilerServices;
using AngleSharp.Common;
using LLMDesktopAssistant.ApiKeys;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using RCLargeLanguageModels;
using Serilog;

namespace LLMDesktopAssistant.Providers
{
	[Service(typeof(IModelManager))]
	public class ModelManager(
		IEnumerable<ModelProviderType> providerTypes,
		IApiKeyManagerService apiKeyManager
	) : IModelManager
	{
		private readonly ModelProvidersConfiguration providers = SettingsManager.Get<ModelProvidersConfiguration>();
		private readonly ModelDescriptorsCache cache = SettingsManager.Get<ModelDescriptorsCache>();
		private readonly Dictionary<string, ModelProviderType> providerTypesMap = providerTypes.ToDictionary(t => t.Id);

		public bool IsModelAvaliable(string fullName)
		{
			var (providerName, modelName) = ParseFullName(fullName);
			var foundProvider = providers.ModelProviders.FirstOrDefault(p => p.Name == providerName);
			if (foundProvider == null)
			{
				Log.Warning("Could not find provider for model {ModelName}: {ProviderName}", modelName, providerName);
				return false;
			}
			return foundProvider.Models.Concat(foundProvider.CustomModels).Any(m => m.Name == modelName);
		}

		public LLModel GetModel(string fullName)
		{
			var (providerName, modelName) = ParseFullName(fullName);
			var client = CreateClient(providerName);
			return new LLModel(client, modelName);
		}

		public IEnumerable<ModelItem> ListModels()
		{
			var cacheLookup = cache.Descriptors.ToDictionary(k => k.Name);
			return providers.ModelProviders.SelectMany(p => p.Models.Concat(p.CustomModels)
				.GroupBy(m => m.Name)
				.Select(g => g.Last())
				.Select(m => new ModelItem
				{
					Provider = p,
					Descriptor = m.IsInformationKnown ? m : cacheLookup.TryGetValue(m.Name, out var cached) ? cached : m,
					FullName = p.Name + "$" + m.Name
				}));
		}

		public async Task<bool> CheckConnectionAsync(ModelProviderConfiguration provider, CancellationToken cancellationToken = default)
		{
			try
			{
				var client = CreateClient(provider);
				await client.ListModelDescriptorsAsync(cancellationToken);
				return true;
			}
			catch (Exception ex)
			{
				Log.Warning(ex, "Failed to check connection for model provider {provider}", provider.Name);
				return false;
			}
		}

		public async Task RefreshModelsAsync(ModelProviderConfiguration provider, CancellationToken cancellationToken = default)
		{
			var client = CreateClient(provider);
			var models = await client.ListModelDescriptorsAsync(cancellationToken);
			provider.Models = [..models.Select(m => ConvertModelFromRCLLM(m))];
		}

		private LLMClient? TryCreateClient(string provider)
		{
			var foundProvider = providers.ModelProviders.FirstOrDefault(p => p.Name == provider);
			if (foundProvider == null)
				return null;
			try
			{
				return CreateClient(foundProvider);
			}
			catch (Exception ex)
			{
				Log.Warning(ex, "Failed to create client for model provider {provider}", provider);
				return null;
			}
		}

		private LLMClient CreateClient(string provider)
		{
			var foundProvider = providers.ModelProviders.FirstOrDefault(p => p.Name == provider);
			if (foundProvider == null)
				throw new ArgumentException($"Provider '{provider}' not found. Please check the name of the provider." +
					$"Expected one of: " + string.Join(", ", providers.ModelProviders.Select(p => p.Name)), nameof(provider));
			return CreateClient(foundProvider);
		}

		private LLMClient CreateClient(ModelProviderConfiguration provider)
		{
			if (!providerTypesMap.TryGetValue(provider.Type, out var providerType))
				throw new InvalidOperationException($"Provider '{provider.Name}' not found in the provider types map."
					+ $"Expected one of: " + string.Join(", ", providerTypesMap.Keys));
			var apiKey = apiKeyManager.GetTokenAccessor(provider.ApiKeyId);
			return providerType.CreateClient(provider, apiKey);
		}

		private static ModelDescriptor ConvertModelFromRCLLM(LLModelDescriptor descriptor)
		{
			return new ModelDescriptor
			{
				Name = descriptor.Name,

				IsInformationKnown = descriptor.Capabilities != LLMCapabilities.Unknown ||
									 descriptor.InputModalities != LLMModalities.Unknown ||
									 descriptor.OutputModalities != LLMModalities.Unknown,

				DisplayName = descriptor.DisplayName,

				InputModalities = descriptor.InputModalities,
				OutputModalities = descriptor.OutputModalities,
				Capabilities = descriptor.Capabilities,

				ContextSize = descriptor.ContextLength,
				MaxOutputTokens = -1,

				InputTokenCost = 0.0m,
				InputCacheTokenCost = 0.0m,
				OutputTokenCost = 0.0m
			};
		}

		private static (string ProviderName, string ModelName) ParseFullName(string fullName)
		{
			var split = fullName.Split('$', count: 2);
			if (split.Length < 2)
				throw new ArgumentException("Invalid model full name format.", nameof(fullName));
			return (ProviderName: split[0], ModelName: split[1]);
		}
	}
}
