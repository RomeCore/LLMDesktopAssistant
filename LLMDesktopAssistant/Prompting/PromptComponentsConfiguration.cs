using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;
using LLTSharp;

namespace LLMDesktopAssistant.Prompting
{
	public class PromptComponentsConfiguration : SettingsObject
	{
		private readonly RangeObservableCollection<PromptComponent> _components = [];
		/// <summary>
		/// Gest or sets the list of prompt components.
		/// </summary>
		public ICollection<PromptComponent> Components
		{
			get => _components;
			set => _components.Reset(value);
		}
	}
}
