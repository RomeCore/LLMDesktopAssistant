using LLMDesktopAssistant.ApiKeys;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Providers
{
	public class ModelProviderConfiguration : NotifyPropertyChanged
	{
		private string _name = string.Empty;
		/// <summary>
		/// The display name of this model provider.
		/// </summary>
		public string Name
		{
			get => _name;
			set => SetProperty(ref _name, value);
		}

		private string _type = string.Empty;
		/// <summary>
		/// The type of the model provider (e.g. OpenAI, Anthropic, Ollama, etc.).
		/// Found by ID from <see cref="ModelProviderType.Id"/> in the APP services.
		/// </summary>
		public string Type
		{
			get => _type;
			set => SetProperty(ref _type, value);
		}

		private string? _endpointUri;
		/// <summary>
		/// The URI of the endpoint where the model provider is hosted. Will be null for default provider's URI.
		/// </summary>
		public string? EndpointUri
		{
			get => _endpointUri;
			set => SetProperty(ref _endpointUri, value);
		}

		private Guid _apiKeyId = Guid.Empty;
		/// <summary>
		/// The unique identifier for the API key associated with this model provider.
		/// Refers to <see cref="IApiKeyManagerService"/> to retrieve the actual API key.
		/// </summary>
		public Guid ApiKeyId
		{
			get => _apiKeyId;
			set => SetProperty(ref _apiKeyId, value);
		}

		private readonly RangeObservableCollection<string> _selectedModelNames = [];
		/// <summary>
		/// A collection of selected model names to show in the model selector.
		/// Only models listed here (plus <see cref="CustomModels"/>) will be displayed.
		/// If empty, all models are shown.
		/// </summary>
		public RangeObservableCollection<string> SelectedModelNames
		{
			get => _selectedModelNames;
			set => _selectedModelNames.Reset(value);
		}


		private readonly RangeObservableCollection<ModelDescriptor> _models = [];
		/// <summary>
		/// A collection of models provided by this model provider.
		/// These are refreshed periodically and can be overriden.
		/// </summary>
		public RangeObservableCollection<ModelDescriptor> Models
		{
			get => _models;
			set => _models.Reset(value);
		}

		private readonly RangeObservableCollection<ModelDescriptor> _customModels = [];
		/// <summary>
		/// A collection of custom models provided by this model provider.
		/// These are not part of the default set of models offered by the provider.
		/// </summary>
		public RangeObservableCollection<ModelDescriptor> CustomModels
		{
			get => _customModels;
			set => _customModels.Reset(value);
		}
	}
}
