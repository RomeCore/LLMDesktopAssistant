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

	public static class ServiceKeys
	{
		public static object? AppServices { get; } = "Main";
		public static object? ChatServices { get; } = "Chat";
	}

	/// <summary>
	/// The manager for application services. All services are statically registered and can be retrieved by their type.
	/// </summary>
	public static class ServiceRegistry
	{
		private static bool _initialized = false;
		private static IServiceProvider _serviceProvider = null!;

		/// <summary>
		/// Gets the service provider for the application. This is used to resolve services by their type.
		/// </summary>
		public static IServiceProvider Provider => _serviceProvider;

		/// <summary>
		/// Add services from <see cref="ServiceRegistry"/> to the provided collection.
		/// </summary>
		/// <param name="services">The collection of services to add.</param>
		/// <returns>The provided collection with services added.</returns>
		public static IServiceCollection AddAppServices(this IServiceCollection services)
		{
			CheckInitialized();

			var originalCollection = _serviceProvider.GetRequiredKeyedService<IServiceCollection>(ServiceKeys.AppServices);
			var serviceTypes = originalCollection.Select(s => s.ServiceType).Distinct();
			var servicesToAdd = serviceTypes.SelectMany(t => _serviceProvider.GetServices(t).Select(s => (t, s))).Distinct();
			foreach (var (serviceType, service) in servicesToAdd)
				if (service != null)
					services.AddSingleton(serviceType, service);

			return services;
		}

		private static void CheckInitialized()
		{
			if (!_initialized)
				throw new InvalidOperationException("ServiceRegistry is not initialized.");
		}

		/// <summary>
		/// Initializes all registered services.
		/// </summary>
		public static void Initialize(IEnumerable<object> services, Action<IServiceCollection> configureServices)
		{
			if (_initialized)
				throw new InvalidOperationException("ModuleManager is already initialized.");

			var collection = new ServiceCollection();

			collection.AddKeyedSingleton<IServiceCollection>(ServiceKeys.AppServices, collection);

			foreach (var service in services)
			{
				collection.AddSingleton(service);
			}

			configureServices?.Invoke(collection);

			foreach (var service in ReflectionUtility.GetTypesWithAttribute<object, ServiceAttribute>())
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

			foreach (var service in collection)
				_serviceProvider.GetServices(service.ServiceType);

			_initialized = true;

			Log.Information("ServiceRegistry initialized with {Count} App services.", collection.Count);
		}
	}
}