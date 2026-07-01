using System;
using System.Collections.Generic;
using System.Text;
using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Providers
{
	public class ModelDescriptorsCache : SettingsObject
	{
		private readonly RangeObservableCollection<ModelDescriptor> _descriptors = [];
		/// <summary>
		/// A collection of model descriptors that contain actual information about the models.
		/// The information inculdes in/out modalities, context window, etc.
		/// </summary>
		public RangeObservableCollection<ModelDescriptor> Descriptors
		{
			get => _descriptors;
			set => _descriptors.Reset(value);
		}
	}
}
