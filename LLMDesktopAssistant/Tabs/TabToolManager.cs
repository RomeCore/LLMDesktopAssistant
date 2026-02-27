using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using LLMDesktopAssistant.MVVM;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Tabs
{
	/// <summary>
	/// Manages tools for the application that can be displayed in tabs.
	/// </summary>
	public static class TabToolManager
	{
		private static readonly ImmutableDictionary<string, Type> _tabTools;

		static TabToolManager()
		{
			_tabTools = ReflectionUtility.GetTypesWithAttribute<object, TabToolAttribute>()
				.ValidateParameterlessConstructors()
				.ToImmutableDictionary(t => t.Attribute.Id, t => t.Type);
		}

		/// <summary>
		/// Instantiates all tools that can be displayed in tabs.
		/// </summary>
		/// <returns>A dictionary of tool IDs and their corresponding instances.</returns>
		public static Dictionary<string, object> Instantiate()
		{
			return _tabTools.ToDictionary(t => t.Key, t =>
			{
				var value = t.Value.Instantiate();
				if (value is FrameworkElement fe)
					return fe;
				return ViewLocator.Resolve(value) ?? value;
			});
		}
	}
}