using LLMDesktopAssistant.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.LLM.Settings
{
	public class ChatLLMPropertiesSettings : NotifyPropertyChanged
	{
		private bool _enableReasoningSettings = false;
		public bool EnableReasoningSettings
		{
			get => _enableReasoningSettings;
			set => SetProperty(ref _enableReasoningSettings, value);
		}

		private ReasoningSettings _reasoningSettings = ReasoningSettings.Disabled;
		public ReasoningSettings ReasoningSettings
		{
			get => _reasoningSettings;
			set => SetProperty(ref _reasoningSettings, value);
		}

		private bool _enableTemperature = false;
		public bool EnableTemperature
		{
			get => _enableTemperature;
			set => SetProperty(ref _enableTemperature, value);
		}

		private float _temperature = 1.0f;
		public float Temperature
		{
			get => _temperature;
			set => SetProperty(ref _temperature, value);
		}

		private RangeObservableCollection<AdditionalParameter> _additionalParameters = [];
		public RangeObservableCollection<AdditionalParameter> AdditionalParameters
		{
			get => _additionalParameters;
			set => _additionalParameters.Reset(value);
		}
	}
}