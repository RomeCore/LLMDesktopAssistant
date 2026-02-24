using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.MVVM
{
	/// <summary>
	/// Represents a view locator for mapping view models to views.
	/// </summary>
	public sealed class ViewLocator : DataTemplateSelector
	{
		private static readonly Dictionary<Type, Type> _viewMappings;

		static ViewLocator()
		{
			_viewMappings = ReflectionUtility.GetTypesWithAttribute<ViewAttribute>()
				.ToDictionary(t => t.Type, t => t.Attribute.TargetView);
		}

		/// <summary>
		/// Selects the appropriate data template based on the view model.
		/// </summary>
		/// <param name="viewModel">The view model.</param>
		/// <returns>The data template for the specified view model.</returns>
		public static object? Resolve(object? viewModel)
		{
			if (viewModel != null && _viewMappings.TryGetValue(viewModel.GetType(), out var viewType))
			{
				var view = Activator.CreateInstance(viewType);
				if (view is FrameworkElement fe)
					fe.DataContext = viewModel;
				return view;
			}
			return null;
		}

		/// <summary>
		/// Determines if a view exists for the specified view model type.
		/// </summary>
		/// <param name="viewModelType">The type of the view model.</param>
		/// <returns>True if a view exists for the specified type; otherwise, false.</returns>
		public static bool HasView(Type viewModelType)
		{
			return _viewMappings.ContainsKey(viewModelType);
		}

		/// <summary>
		/// Determines if a view exists for the specified view model type.
		/// </summary>
		/// <typeparam name="T">The type of the view model.</typeparam>
		/// <returns>True if a view exists for the specified type; otherwise, false.</returns>
		public static bool HasView<T>()
		{
			return _viewMappings.ContainsKey(typeof(T));
		}

		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			if (item != null && _viewMappings.TryGetValue(item.GetType(), out var viewType))
			{
				var template = new DataTemplate();
				template.VisualTree = new FrameworkElementFactory(viewType);
				return template;
			}
			return base.SelectTemplate(item, container);
		}
	}
}