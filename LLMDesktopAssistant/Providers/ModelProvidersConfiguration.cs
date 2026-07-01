using System;
using System.Collections.Generic;
using System.Text;
using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Providers
{
	public class ModelProvidersConfiguration : SettingsObject
	{
		private RangeObservableCollection<ModelProviderConfiguration> _modelProviders = [];
		public RangeObservableCollection<ModelProviderConfiguration> ModelProviders
		{
			get => _modelProviders;
			set => _modelProviders.Reset(value);
		}
	}
}
