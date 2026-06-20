using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.ApiKeys
{
	public class ApiKeysConfiguration : SettingsObject
	{
		private RangeObservableCollection<ApiKeysConfigurationItem> _apiKeys = [];
		public RangeObservableCollection<ApiKeysConfigurationItem> ApiKeys
		{
			get => _apiKeys;
			set => _apiKeys.Reset(value);
		}
	}
}
