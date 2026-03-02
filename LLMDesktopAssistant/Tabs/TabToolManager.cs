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
		private static readonly ImmutableDictionary<string, object> _tabTools;
		private static readonly ImmutableDictionary<string, object> _tabToolViews;

		static TabToolManager()
		{
			var tabTools = ReflectionUtility.GetTypesWithAttribute<object, TabToolAttribute>()
				.ValidateParameterlessConstructors()
				.ToImmutableDictionary(t => t.Attribute.Id, t => t.Type);

			_tabTools = tabTools.ToImmutableDictionary(t => t.Key, t =>
			{
				var value = t.Value.Instantiate();
				return value;
			});

			_tabToolViews = _tabTools.ToImmutableDictionary(t => t.Key, t =>
			{
				var value = t.Value;
				if (value is FrameworkElement fe)
					return fe;
				return ViewLocator.Resolve(value) ?? value;
			});
		}

		/// <summary>
		/// Gets the instances of all tab tools.
		/// </summary>
		public static ImmutableDictionary<string, object> TabTools => _tabTools;

		/// <summary>
		/// Gets the instances of all tab tool views.
		/// </summary>
		public static ImmutableDictionary<string, object> TabToolViews => _tabToolViews;

		/// <summary>
		/// Gets the instance of a specific tab tool.
		/// </summary>
		/// <typeparam name="T">The type of the tool.</typeparam>
		/// <param name="id">The ID of the tool.</param>
		/// <returns>The instance of the tool.</returns>
		/// <exception cref="InvalidCastException">Thrown when the tool cannot be cast to the specified type.</exception>
		/// <exception cref="KeyNotFoundException">Thrown when no tool is found with the specified ID.</exception>
		public static T Get<T>(string id)
		{
			if (_tabTools.TryGetValue(id, out var tabTool))
			{
				if (tabTool is T result)
					return result;
				throw new InvalidCastException($"Cannot cast '{tabTool.GetType()}' to '{typeof(T)}'.");
			}
			throw new KeyNotFoundException($"No tool found with id '{id}'.");
		}

		/// <summary>
		/// Gets the view instance of a specific tab tool.
		/// </summary>
		/// <typeparam name="T">The type of the tool.</typeparam>
		/// <param name="id">The ID of the tool.</param>
		/// <returns>The view instance of the tool.</returns>
		/// <exception cref="InvalidCastException">Thrown when the tool cannot be cast to the specified type.</exception>
		/// <exception cref="KeyNotFoundException">Thrown when no tool is found with the specified ID.</exception>
		public static T GetView<T>(string id)
		{
			if (_tabToolViews.TryGetValue(id, out var tabTool))
			{
				if (tabTool is T result)
					return result;
				throw new InvalidCastException($"Cannot cast '{tabTool.GetType()}' to '{typeof(T)}'.");
			}
			throw new KeyNotFoundException($"No tool found with id '{id}'.");
		}
	}
}