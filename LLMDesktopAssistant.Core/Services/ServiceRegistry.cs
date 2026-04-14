using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.Core.Utils;
using LLTSharp;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace LLMDesktopAssistant.Core.Services
{
#pragma warning disable CS8631

	/// <summary>
	/// The manager for application services. All services are statically registered and can be retrieved by their type.
	/// </summary>
	public static class ServiceRegistry
	{
		private enum State
		{
			None,
			Initialized,
			Shutdown
		}

		private static State _state;
		private static IServiceProvider _serviceProvider = null!;
		private static ImmutableUniqueTypeDictionary<object> _services = null!;
		private static ImmutableDictionary<Type, ImmutableList<DynamicServiceTypeInfo>> _dynamicRegistry = null!;
		private static ConcurrentDictionary<Type, DynamicServiceTracker> _dynamicTrackers = null!;

		/// <summary>
		/// Gets the service provider for the application. This is used to resolve services by their type.
		/// </summary>
		public static IServiceProvider Provider => _serviceProvider;

		/// <summary>
		/// Gets the collection of all registered services.
		/// </summary>
		public static ITypeDictionary<object> Services
		{
			get
			{
				CheckInitialized();
				return _services;
			}
		}

		/// <summary>
		/// Add services from <see cref="ServiceRegistry"/> to the provided collection.
		/// </summary>
		/// <param name="services">The collection of services to add.</param>
		/// <returns>The provided collection with services added.</returns>
		public static IServiceCollection AddAppServices(this IServiceCollection services)
		{
			CheckInitialized();

			foreach (var service in Services)
				services.AddSingleton(service);

			return services;
		}

		private static void CheckInitialized()
		{
			switch (_state)
			{
				case State.None:
					throw new InvalidOperationException("ServiceRegistry is not initialized.");
				case State.Initialized:
					return;
				case State.Shutdown:
					throw new InvalidOperationException("ServiceRegistry is already shutdown.");
				default:
					throw new InvalidOperationException("Invalid state.");
			}
		}

		/// <summary>
		/// Initializes all registered services.
		/// </summary>
		public static void Initialize(IEnumerable<object> services)
		{
			if (_state != State.None)
				throw new InvalidOperationException("ModuleManager is already initialized.");

			var collection = new ServiceCollection();
			var serviceTypes = new HashSet<Type>();
			
			foreach (var service in ReflectionUtility.GetTypesWithAttribute<object, ServiceAttribute>()
				.OrderBy(t => t.Attribute.Order))
			{
				var serviceType = service.Attribute.ServiceType ?? service.Type;
				serviceTypes.Add(serviceType);
				collection.AddSingleton(serviceType, service.Type);
			}
			foreach (var service in services)
			{
				collection.AddSingleton(service);
			}

			_serviceProvider = collection.BuildServiceProvider();

			_dynamicRegistry = ReflectionUtility.GetTypesWithAttribute<IDynamicService, DynamicServiceAttribute>()
				.OrderBy(t => t.Attribute.Order)
				.GroupBy(t => t.Attribute.CategoryType)
				.ToImmutableDictionary(g => g.Key, g => g
					.Select(t => new DynamicServiceTypeInfo(t.Attribute.Id, t.Type, t.Attribute.Order))
					.ToImmutableList());

			// Validate

			var invalidDynModules = new List<string>();
			foreach (var (categoryType, category) in _dynamicRegistry)
			{
				foreach (var typeInfo in category)
				{
					if (!categoryType.IsAssignableFrom(typeInfo.Type))
						invalidDynModules.Add($"Dynamic module '{typeInfo.Type.Name}' with ID '{typeInfo.Id}' cannod be assigned to category '{categoryType.Name}'.");
				}
			}
			if (invalidDynModules.Count > 0)
				throw new Exception("Invalid dynamic modules found: " + string.Join(", ", invalidDynModules));

			_state = State.Initialized;

			_dynamicTrackers = new(
				_dynamicRegistry.ToDictionary(t => t.Key,
				t =>
				{
					var trackerType = typeof(DynamicServiceTracker<>).MakeGenericType(t.Key);
					return (DynamicServiceTracker)ActivatorUtilities.CreateInstance(_serviceProvider, trackerType);
				}));

			var allServices = new List<object>();
			foreach (var serviceType in serviceTypes)
				allServices.AddRange(_serviceProvider.GetServices(serviceType)!);
			_services = new(allServices.Distinct().Where(s => s != null));

			foreach (var tracker in _dynamicTrackers.Values)
				tracker.Initialize();
		}

		/// <summary>
		/// Shuts down all registered modules.
		/// </summary>
		public static void Shutdown()
		{
			if (_state != State.Initialized)
				throw new InvalidOperationException("ModuleManager is not initialized or already shut down.");

			_services = null!;

			foreach (var tracker in _dynamicTrackers.Values)
				tracker.NonGenericModule?.Shutdown();

			_state = State.Shutdown;
		}

		/// <summary>
		/// Gets a module of the specified type.
		/// </summary>
		/// <typeparam name="T">The type of the module to retrieve.</typeparam>
		/// <returns>The module, or null if no such module is registered.</returns>
		public static T? TryGet<T>()
		{
			if (_state == State.Initialized)
				return _services.TryGet<T>();
			return default;
		}

		/// <summary>
		/// Gets a module of the specified type.
		/// </summary>
		/// <typeparam name="T">The type of the module to retrieve.</typeparam>
		/// <returns>The module of the specified type, or throws an exception if no such module is registered.</returns>
		public static T Get<T>()
		{
			CheckInitialized();
			return _services.TryGet<T>() ?? throw new ServiceNotFoundException($"No module of type '{typeof(T).FullName}' is registered.");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static IEnumerable<T> GetAll<T>()
		{
			if (_state == State.Initialized)
				return _services.GetAll<T>();
			return [];
		}

		/// <summary>
		/// Gets a dynamic module of the specified category type. Throws an exception if no such category type is registered.
		/// </summary>
		/// <typeparam name="T">The category type of the dynamic module to retrieve.</typeparam>
		/// <returns>The dynamic module.</returns>
		/// <exception cref="ServiceNotFoundException">The specified category type is not registered.</exception>
		public static T? TryGetDynamic<T>()
			where T : IDynamicService
		{
			return GetDynamicTracker<T>().Module;
		}

		/// <summary>
		/// Gets a dynamic module of the specified category type. Throws an exception if no such category type is registered.
		/// </summary>
		/// <typeparam name="T">The category type of the dynamic module to retrieve.</typeparam>
		/// <returns>The dynamic module.</returns>
		/// <exception cref="ServiceNotFoundException">The specified category type is not registered.</exception>
		public static T GetDynamic<T>()
			where T : IDynamicService
		{
			return GetDynamicTracker<T>().Module ?? throw new ServiceNotFoundException($"No module of category type '{typeof(T).FullName}' is present.");
		}

		/// <summary>
		/// Gets a dynamic module tracker of the specified type. Throws an exception if no such category type is registered.
		/// </summary>
		/// <typeparam name="T">The category type of the dynamic module tracker to retrieve.</typeparam>
		/// <returns>The dynamic module tracker.</returns>
		/// <exception cref="ServiceNotFoundException">The specified category type is not registered.</exception>
		public static DynamicServiceTracker<T> GetDynamicTracker<T>()
			where T : IDynamicService
		{
			CheckInitialized();
			return (DynamicServiceTracker<T>)_dynamicTrackers.GetOrAdd(typeof(T), static categoryType =>
			{
				var trackerType = typeof(DynamicServiceTracker<>).MakeGenericType(typeof(T));
				return (DynamicServiceTracker<T>)ActivatorUtilities.CreateInstance(_serviceProvider, trackerType);
			});
		}
	}
}