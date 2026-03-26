using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using LLMDesktopAssistant.MVVM;
using LLMDesktopAssistant.Utils;
using MaterialDesignThemes.Wpf;

namespace LLMDesktopAssistant.Tabs
{
	public class TabToolInfo
	{
		public required string Id { get; init; }
		public required PackIconKind Icon { get; init; }
		public required int Order { get; init; }
		public required object Content { get; init; }
		public required object View { get; init; }
	}

	/// <summary>
	/// Manages tools for the application that can be displayed in tabs.
	/// </summary>
	public static class TabToolManager
	{
		private static readonly ImmutableDictionary<string, TabToolInfo> _tabTools;

		static TabToolManager()
		{
			var tabTools = ReflectionUtility.GetTypesWithAttribute<object, TabToolAttribute>()
				.ValidateParameterlessConstructors()
				.OrderBy(t => t.Attribute.Order)
				.ToImmutableDictionary(t => t.Attribute.Id, t => t);

			_tabTools = tabTools.ToImmutableDictionary(t => t.Key, t =>
			{
				var value = t.Value.Type.Instantiate();
				var view = value is FrameworkElement fe ? fe : ViewLocator.Resolve(value) ?? value;

				return new TabToolInfo
				{
					Id = t.Key,
					Icon = t.Value.Attribute.Icon,
					Order = t.Value.Attribute.Order,
					Content = value,
					View = view
				};
			});
		}

		/// <summary>
		/// Gets the instances of all tab tools.
		/// </summary>
		public static ImmutableDictionary<string, TabToolInfo> TabTools => _tabTools;

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
				if (tabTool.Content is T result)
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
			if (_tabTools.TryGetValue(id, out var tabTool))
			{
				if (tabTool.View is T result)
					return result;
				throw new InvalidCastException($"Cannot cast '{tabTool.GetType()}' to '{typeof(T)}'.");
			}
			throw new KeyNotFoundException($"No tool found with id '{id}'.");
		}
	}
}