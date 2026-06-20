namespace LLMDesktopAssistant.ApiKeys
{
	public class ApiKeysConfigurationItem : NotifyPropertyChanged
	{
		private Guid _id = Guid.NewGuid();
		public Guid Id
		{
			get => _id;
			set => SetProperty(ref _id, value);
		}

		private string _name = string.Empty;
		/// <summary>
		/// Display name or label for this API key (e.g. "OpenAI Production", "DeepSeek Personal").
		/// </summary>
		public string Name
		{
			get => _name;
			set => SetProperty(ref _name, value);
		}

		private string? _storedValue = null;
		public string? StoredValue
		{
			get => _storedValue;
			set => SetProperty(ref _storedValue, value);
		}

		private ApiKeyStorageScheme _storageScheme = ApiKeyStorageScheme.Raw;
		public ApiKeyStorageScheme StorageScheme
		{
			get => _storageScheme;
			set => SetProperty(ref _storageScheme, value);
		}
	}
}
