using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.Utils;
using LLTSharp;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace LLMDesktopAssistant.Services
{
#pragma warning disable CS8631

	/// <summary>
	/// The manager for application services. All services are statically registered and can be retrieved by their type.
	/// </summary>
	public static class ServiceRegistry
	{
		public static object? AppServicesKey { get; } = "Main";
		public static object? ChatServicesKey { get; } = "Chat";

		private enum State
		{
			None,
			Initialized,
			Shutdown
		}

		private static State _state;
		private static IServiceProvider _serviceProvider = null!;
		private static ImmutableUniqueTypeDictionary<object> _services = null!;

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

			var originalCollection = _serviceProvider.GetRequiredKeyedService<IServiceCollection>(AppServicesKey);
			foreach (var service in originalCollection)
				if (!service.IsKeyedService)
					services.AddSingleton(service.ServiceType, _serviceProvider.GetRequiredService(service.ServiceType));

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
		public static void Initialize(IEnumerable<object> services, Action<IServiceCollection> configureServices)
		{
			if (_state != State.None)
				throw new InvalidOperationException("ModuleManager is already initialized.");

			var collection = new ServiceCollection();

			collection.AddKeyedSingleton<IServiceCollection>(AppServicesKey, collection);

			foreach (var service in services)
			{
				collection.AddSingleton(service);
			}

			configureServices?.Invoke(collection);

			foreach (var service in ReflectionUtility.GetTypesWithAttribute<object, ServiceAttribute>()
				.OrderBy(t => t.Attribute.Order))
			{
				var serviceType = service.Attribute.ServiceType ?? service.Type;
				collection.AddSingleton(serviceType, service.Type);
			}

			foreach (var configurator in ReflectionUtility.GetTypesWithAttribute<ServiceConfigurator, ServiceConfiguratorAttribute>())
			{
				if (configurator.Attribute.Scope == ServiceScope.App)
					configurator.Type.Instantiate<ServiceConfigurator>().Configure(collection);
			}

			_serviceProvider = collection.BuildServiceProvider();

			// Validate

			_state = State.Initialized;

			var allServices = new List<object?>();
			foreach (var service in collection)
				allServices.AddRange(_serviceProvider.GetServices(service.ServiceType));
			_services = new(allServices.Where(s => s != null).Distinct()!);

			Log.Information("ServiceRegistry initialized with {Count} App services.", collection.Count);
		}

		/// <summary>
		/// Shuts down all registered modules.
		/// </summary>
		public static void Shutdown()
		{
			if (_state != State.Initialized)
				throw new InvalidOperationException("ModuleManager is not initialized or already shut down.");

			_services = null!;

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
			CheckInitialized();
			return _services.GetAll<T>();
		}
	}
}