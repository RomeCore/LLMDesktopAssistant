using Avalonia.Controls;
using Avalonia.Controls.Templates;
using LLMDesktopAssistant.Utils;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LLMDesktopAssistant.MVVM
{
	/// <summary>
	/// Represents a view locator for mapping view models to views.
	/// </summary>
	public sealed class ViewLocator : IDataTemplate
	{
		private static readonly Dictionary<Type, Type> _ViewModel_to_View_map;
		private static readonly Dictionary<Type, Type> _View_to_ViewModel_map;

		static ViewLocator()
		{
			List<(Type ViewModelType, Type ViewType)> mappings = [];

			// Try-catch block needed for designer support
			try
			{
				mappings = ReflectionUtility.GetTypesWithAttribute<ViewModelForAttribute>()
					.Select(t => (t.Type, t.Attribute.TargetView))
					.Concat(
						ReflectionUtility.GetTypesWithAttribute<ViewForAttribute>()
							.Select(t => (t.Attribute.TargetViewModel, t.Type))
					)
					.Distinct()
					.ToList();
			}
			catch
			{
			}

			// Validate mappings for correctness (1-to-1 correspondence between view models and views).

			var viewModelToView = mappings.ToLookup(t => t.ViewModelType, t => t.ViewType);
			var viewToViewModel = mappings.ToLookup(t => t.ViewType, t => t.ViewModelType);

			var errors = new List<string>();

			foreach (var group in viewModelToView.Where(g => g.Count() > 1))
				errors.Add($"Multiple views found for view model type {group.Key}.");

			foreach (var group in viewToViewModel.Where(g => g.Count() > 1))
				errors.Add($"Multiple view models found for view type {group.Key}.");

			if (errors.Count > 0)
			{
				foreach (var error in errors)
					Log.Error(error);
				throw new InvalidOperationException($"ViewLocator initialization failed:\n{string.Join("\n", errors)}");
			}

			_ViewModel_to_View_map = mappings.ToDictionary(t => t.ViewModelType, t => t.ViewType);
			_View_to_ViewModel_map = mappings.ToDictionary(t => t.ViewType, t => t.ViewModelType);
		}

		/// <summary>
		/// Gets the singleton instance of the <see cref="ViewLocator"/> class.
		/// </summary>
		public static ViewLocator Instance { get; } = new ViewLocator();

		/// <summary>
		/// Resolves the view type for a given view model type.
		/// </summary>
		/// <param name="viewModelType">The type of the view model.</param>
		/// <returns>The type of the view, or null if no mapping is found.</returns>
		public static Type? ResolveViewType(Type? viewModelType)
		{
			if (viewModelType == null)
				return null;

			if (_ViewModel_to_View_map.TryGetValue(viewModelType, out var viewType))
				return viewType;

			if (viewModelType.IsGenericType)
			{
				viewModelType = viewModelType.GetGenericTypeDefinition();
				if (_ViewModel_to_View_map.TryGetValue(viewModelType, out viewType))
					return viewType;
			}

			return null;
		}

		/// <summary>
		/// Selects the appropriate data template based on the view model.
		/// </summary>
		/// <param name="viewModel">The view model.</param>
		/// <returns>The data template for the specified view model.</returns>
		public static object? Resolve(object? viewModel)
		{
			if (ResolveViewType(viewModel?.GetType()) is Type viewType)
			{
				var view = Activator.CreateInstance(viewType);
				if (view is Control fe)
					fe.DataContext = viewModel;
				return view;
			}
			return null;
		}

		/// <summary>
		/// Sets the data context of a view to its corresponding view model.
		/// </summary>
		/// <param name="view">The view to set the data context (view model) for.</param>
		/// <returns>The data context (view model) for the specified view.</returns>
		public static object? SetViewModelTo(Control? view)
		{
			if (view != null && _View_to_ViewModel_map.TryGetValue(view.GetType(), out var viewModelType))
			{
				var viewModel = Activator.CreateInstance(viewModelType);
				view.DataContext = viewModel;
				return viewModel;
			}
			return null;
		}

		/// <summary>
		/// Gets the view model type for a specified view.
		/// </summary>
		/// <param name="view">The view to get the view model type for.</param>
		/// <returns>The view model type for the specified view.</returns>
		public static Type? GetViewModelTypeFor(Control view)
		{
			if (view != null && _View_to_ViewModel_map.TryGetValue(view.GetType(), out var viewModelType))
				return viewModelType;
			return null;
		}

		/// <summary>
		/// Gets the view model type for a specified view type.
		/// </summary>
		/// <param name="viewType">The type of view to get the view model type for.</param>
		/// <returns>The view model type for the specified view type.</returns>
		public static Type? GetViewModelTypeFor(Type viewType)
		{
			if (viewType != null && _View_to_ViewModel_map.TryGetValue(viewType, out var viewModelType))
			{
				return viewModelType;
			}
			return null;
		}

		/// <summary>
		/// Gets the view model for a specified view.
		/// </summary>
		/// <param name="view">The view to get the view model for.</param>
		/// <returns>The view model for the specified view.</returns>
		public static object? GetViewModelFor(Control view)
		{
			if (view != null && _View_to_ViewModel_map.TryGetValue(view.GetType(), out var viewModelType))
			{
				var viewModel = Activator.CreateInstance(viewModelType);
				return viewModel;
			}
			return null;
		}

		/// <summary>
		/// Gets the view model for a specified view type.
		/// </summary>
		/// <param name="viewType">The type of view to get the view model for.</param>
		/// <returns>The view model for the specified view type.</returns>
		public static object? GetViewModelFor(Type viewType)
		{
			if (viewType != null && _View_to_ViewModel_map.TryGetValue(viewType, out var viewModelType))
			{
				var viewModel = Activator.CreateInstance(viewModelType);
				return viewModel;
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
			return _ViewModel_to_View_map.ContainsKey(viewModelType);
		}

		/// <summary>
		/// Determines if a view exists for the specified view model type.
		/// </summary>
		/// <typeparam name="T">The type of the view model.</typeparam>
		/// <returns>True if a view exists for the specified type; otherwise, false.</returns>
		public static bool HasView<T>()
		{
			return _ViewModel_to_View_map.ContainsKey(typeof(T));
		}

		public Control Build(object? data)
		{
			return Resolve(data) as Control ?? new Label() { Content = "No view found for this model." };
		}

		public bool Match(object? data)
		{
			return data is not null && HasView(data.GetType());
		}
	}
}