using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LLMDesktopAssistant.Services
{
	/// <summary>
	/// Provides extension methods for <see cref="IServiceCollection"/>.
	/// </summary>
	public static class ServiceCollectionExtensions
	{
		/// <summary>
		/// Deduplicates type-based service registrations so that the same implementation type
		/// registered under multiple service types (for example, via multiple attributes)
		/// resolves to a single shared instance.
		/// </summary>
		/// <remarks>
		/// Microsoft DI does not deduplicate registrations: singleton/scoped caching is keyed by
		/// the service type, so registering one implementation under two interfaces produces two
		/// independent instances. This method groups registrations by implementation type and
		/// lifetime, keeps the concrete-type registration as the single instance owner and
		/// forwards every interface registration to it.
		/// Only type-based registrations (<see cref="ServiceDescriptor.ImplementationType"/>) are
		/// deduplicated. Factory-based registrations
		/// (<see cref="ServiceDescriptor.ImplementationFactory"/>) and instance-based registrations
		/// (<see cref="ServiceDescriptor.ImplementationInstance"/>) are passed through unchanged:
		/// instances are already shared, and factories are intentionally left untouched because
		/// deduplicating them would require invoking them eagerly. Keyed registrations
		/// (<see cref="ServiceDescriptor.IsKeyedService"/>) are passed through unchanged as well.
		/// Registrations of the same implementation type with different lifetimes are not merged.
		/// </remarks>
		/// <param name="services">The service collection to deduplicate.</param>
		/// <returns>The same service collection, rebuilt with deduplicated registrations.</returns>
		public static IServiceCollection DeduplicateServices(this IServiceCollection services)
		{
			var descriptors = services.Select((d, i) => (Index: i, Descriptor: d)).ToArray();
			var result = new List<(int Index, ServiceDescriptor Descriptor)>(descriptors.Length);

			foreach (var group in descriptors
				.Where(d => d.Descriptor.ImplementationType != null && !d.Descriptor.IsKeyedService)
				.GroupBy(d => (ImplementationType: d.Descriptor.ImplementationType!, Lifetime: d.Descriptor.Lifetime)))
			{
				var implementationType = group.Key.ImplementationType;
				var lifetime = group.Key.Lifetime;
				var registrations = group.ToArray();

				if (registrations.Length == 1)
				{
					result.Add(registrations[0]);
					continue;
				}

				int minIndex = registrations.Min(d => d.Index);

				// Register the concrete type once; every interface forwards to it so that all
				// service types of this implementation share a single instance.
				if (!registrations.Any(d => d.Descriptor.ServiceType == implementationType))
					result.Add((minIndex, ServiceDescriptor.Describe(implementationType, implementationType, lifetime)));

				foreach (var descriptor in registrations)
				{
					if (descriptor.Descriptor.ServiceType == implementationType)
						result.Add((minIndex, descriptor.Descriptor));
					else
						result.Add((minIndex, ServiceDescriptor.Describe(
							descriptor.Descriptor.ServiceType,
							sp => sp.GetRequiredService(implementationType),
							descriptor.Descriptor.Lifetime)));
				}
			}

			foreach (var descriptor in descriptors.Where(d => d.Descriptor.ImplementationType == null || d.Descriptor.IsKeyedService))
				result.Add(descriptor);

			services.Clear();
			foreach (var r in result.OrderBy(r => r.Index))
				services.Add(r.Descriptor);

			return services;
		}
	}
}
