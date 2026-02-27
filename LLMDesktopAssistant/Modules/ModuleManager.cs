using System;
using System.Collections.Concurrent;
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
		private static ImmutableDictionary<Type, ImmutableList<DynamicModuleTypeInfo>> _dynamicModuleRegistry = null!;
		private static ConcurrentDictionary<Type, DynamicModuleTracker> _dynamicModuleTrackers = null!;
		
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
				.ValidateParameterlessConstructors()
				.OrderBy(t => t.Attribute.Order)
				.Select(t => t.Type)
				.Instantiate<IModule>((t, ex) =>
				{
					Log.Error(ex, "Failed to instantiate module {type}: {ex}.", t, ex.Message);
				})
				.ToList();

			_dynamicModuleRegistry = ReflectionUtility.GetTypesWithAttribute<IDynamicModule, DynamicModuleAttribute>()
				.ValidateParameterlessConstructors()
				.OrderBy(t => t.Attribute.Order)
				.GroupBy(t => t.Attribute.CategoryType)
				.ToImmutableDictionary(g => g.Key, 
					g => g.Select(v => new DynamicModuleTypeInfo(v.Attribute.Id, v.Type, v.Attribute.DefaultPriority))
						.ToImmutableList());

			// Validate

			var invalidDynModules = new List<string>();
			foreach (var (categoryType, category) in _dynamicModuleRegistry)
			{
				foreach (var typeInfo in category)
				{
					if (!categoryType.IsAssignableFrom(typeInfo.Type))
						invalidDynModules.Add($"Dynamic module '{typeInfo.Type.Name}' with ID '{typeInfo.Id}' cannod be assigned to category '{categoryType.Name}'.");
				}
			}
			if (invalidDynModules.Count > 0)
				throw new Exception("Invalid dynamic modules found: " + string.Join(", ", invalidDynModules));

			_dynamicModuleTrackers = new(
				_dynamicModuleRegistry.ToDictionary(t => t.Key,
				t =>
				{
					var trackerType = typeof(DynamicModuleTracker<>).MakeGenericType(t.Key);
					var tracker = (DynamicModuleTracker)Activator.CreateInstance(trackerType, t.Value)!;
					return tracker;
				}));

			var validModules = new List<IModule>();
			foreach (var module in modules)
			{
				try
				{
					module.Initialize();
					validModules.Add(module);
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Failed initializing module '{module}': {errmsg}\n{st}", module, ex.Message, ex.StackTrace);
				}
			}
			_modules = new(validModules);

			foreach (var tracker in _dynamicModuleTrackers.Values)
				tracker.Initialize();

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
				tracker.NonGenericModule?.Shutdown();

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
			if (_state == State.Initialized)
				return _modules.TryGet<T>();
			return default;
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
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static IEnumerable<T> GetAll<T>()
			where T : IModule
		{
			if (_state == State.Initialized)
				return _modules.GetAll<T>();
			return [];
		}

		/// <summary>
		/// Gets a dynamic module of the specified category type. Throws an exception if no such category type is registered.
		/// </summary>
		/// <typeparam name="T">The category type of the dynamic module to retrieve.</typeparam>
		/// <returns>The dynamic module.</returns>
		/// <exception cref="ModuleNotFoundException">The specified category type is not registered.</exception>
		public static T? TryGetDynamic<T>()
			where T : IDynamicModule
		{
			return GetDynamicTracker<T>().Module;
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
			return GetDynamicTracker<T>().Module ?? throw new ModuleNotFoundException($"No module of category type '{typeof(T).FullName}' is present.");
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
			CheckInitialized();
			return (DynamicModuleTracker<T>)_dynamicModuleTrackers.GetOrAdd(typeof(T), static categoryType =>
			{
				var trackerType = typeof(DynamicModuleTracker<>).MakeGenericType(typeof(T));
				return (DynamicModuleTracker<T>)Activator.CreateInstance(trackerType, Enumerable.Empty<DynamicModuleTypeInfo>())!;
			});
		}
	}
}