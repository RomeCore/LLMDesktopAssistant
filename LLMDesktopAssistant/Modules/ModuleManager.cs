using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.Utils;
using LLTSharp;
using Serilog;

namespace LLMDesktopAssistant.Modules
{
	/// <summary>
	/// The manager for application modules. All modules are statically registered and can be retrieved by their type.
	/// </summary>
	public static class ModuleManager
	{
		private enum State
		{
			None,
			Initialized,
			Shutdown
		}

		private static State _state;
		private static ImmutableUniqueTypeDictionary<IModule> _modules = null!;
		private static ImmutableDictionary<Type, ImmutableDictionary<string, Type>> _dynamicModuleRegistry = null!;
		private static ImmutableDictionary<Type, DynamicModuleTracker> _dynamicModuleTrackers = null!;
		
		/// <summary>
		/// Gets the collection of all registered modules.
		/// </summary>
		public static ITypeDictionary<IModule> Modules
		{
			get
			{
				CheckInitialized();
				return _modules;
			}
		}

		private static void CheckInitialized()
		{
			switch (_state)
			{
				case State.None:
					throw new InvalidOperationException("ModuleManager is not initialized.");
				case State.Initialized:
					return;
				case State.Shutdown:
					throw new InvalidOperationException("ModuleManager is already shutdown.");
				default:
					throw new InvalidOperationException("Invalid state.");
			}
		}

		/// <summary>
		/// Initializes all registered modules.
		/// </summary>
		public static void Initialize()
		{
			if (_state != State.None)
				throw new InvalidOperationException("ModuleManager is already initialized.");

			var modules = ReflectionUtility.GetTypesWithAttribute<IModule, ModuleAttribute>()
				.OrderBy(t => t.Attribute.Order)
				.Select(t => t.Type)
				.Instantiate<IModule>((t, ex) =>
				{
					Log.Error(ex, "Failed to instantiate module {type}: {ex}.", t, ex.Message);
				})
				.ToList();

			_modules = new ImmutableUniqueTypeDictionary<IModule>(modules);

			_dynamicModuleRegistry = ReflectionUtility.GetTypesWithAttribute<IDynamicModule, DynamicModuleAttribute>()
				.OrderBy(t => t.Attribute.Order)
				.GroupBy(t => t.Attribute.CategoryType)
				.ToImmutableDictionary(g => g.Key, 
					g => g.ToImmutableDictionary(t => t.Attribute.Id, t => t.Type));

			// Validate

			var invalidDynModules = new List<string>();
			foreach (var (categoryType, category) in _dynamicModuleRegistry)
			{
				foreach (var (id, type) in category)
				{
					if (!categoryType.IsAssignableFrom(type))
						invalidDynModules.Add($"Dynamic module '{type.Name}' with ID '{id}' cannod be assigned to category '{categoryType.Name}'.");
				}
			}
			if (invalidDynModules.Count > 0)
				throw new Exception("Invalid dynamic modules found: " + string.Join(", ", invalidDynModules));

			_dynamicModuleTrackers = _dynamicModuleRegistry.ToImmutableDictionary(t => t.Key,
				t =>
				{
					var trackerType = typeof(DynamicModuleTracker<>).MakeGenericType(t.Key);
					var tracker = (DynamicModuleTracker)Activator.CreateInstance(trackerType, t.Value)!;
					return tracker;
				});

			foreach (var module in _modules)
				module.Initialize();

			foreach (var tracker in _dynamicModuleTrackers.Values)
				tracker.NonGenericModule.Initialize();

			_state = State.Initialized;
		}

		/// <summary>
		/// Shuts down all registered modules.
		/// </summary>
		public static void Shutdown()
		{
			if (_state != State.Initialized)
				throw new InvalidOperationException("ModuleManager is not initialized or already shut down.");

			foreach (var module in Modules)
				module.Shutdown();

			foreach (var tracker in _dynamicModuleTrackers.Values)
				tracker.NonGenericModule.Shutdown();

			_state = State.Shutdown;
		}

		/// <summary>
		/// Gets a module of the specified type.
		/// </summary>
		/// <typeparam name="T">The type of the module to retrieve.</typeparam>
		/// <returns>The module, or null if no such module is registered.</returns>
		public static T? TryGet<T>()
			where T : IModule
		{
			CheckInitialized();
			return _modules.TryGet<T>();
		}

		/// <summary>
		/// Gets a module of the specified type.
		/// </summary>
		/// <typeparam name="T">The type of the module to retrieve.</typeparam>
		/// <returns>The module of the specified type, or throws an exception if no such module is registered.</returns>
		public static T Get<T>()
			where T : IModule
		{
			CheckInitialized();
			return _modules.TryGet<T>() ?? throw new ModuleNotFoundException($"No module of type '{typeof(T).FullName}' is registered.");
		}

		/// <summary>
		/// Gets a dynamic module of the specified category type. Throws an exception if no such category type is registered.
		/// </summary>
		/// <typeparam name="T">The category type of the dynamic module to retrieve.</typeparam>
		/// <returns>The dynamic module.</returns>
		/// <exception cref="ModuleNotFoundException">The specified category type is not registered.</exception>
		public static T GetDynamic<T>()
			where T : IDynamicModule
		{
			return GetDynamicTracker<T>().Module;
		}

		/// <summary>
		/// Gets a dynamic module tracker of the specified type. Throws an exception if no such category type is registered.
		/// </summary>
		/// <typeparam name="T">The category type of the dynamic module tracker to retrieve.</typeparam>
		/// <returns>The dynamic module tracker.</returns>
		/// <exception cref="ModuleNotFoundException">The specified category type is not registered.</exception>
		public static DynamicModuleTracker<T> GetDynamicTracker<T>()
			where T : IDynamicModule
		{
			var categoryType = typeof(T);
			if (!_dynamicModuleTrackers.TryGetValue(categoryType, out var _tracker))
				throw new ModuleNotFoundException($"No dynamic module of category '{typeof(T).FullName}' is registered.");

			var tracker = (DynamicModuleTracker<T>)_tracker;
			return tracker;
		}
	}
}