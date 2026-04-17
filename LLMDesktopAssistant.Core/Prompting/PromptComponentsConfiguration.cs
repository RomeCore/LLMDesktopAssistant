using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.Core.Settings;
using LLMDesktopAssistant.Core.Utils;
using LLTSharp;

namespace LLMDesktopAssistant.Core.Prompting
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
