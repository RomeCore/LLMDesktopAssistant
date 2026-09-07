using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Addons.Loading;
using LLMDesktopAssistant.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace LLMDesktopAssistant.Services.Configurators
{
	/// <summary>
	/// Registers closed generic addon services for every <see cref="IAddonTypeDescriptor"/> as app singletons:
	/// <see cref="IAddonAccessor{T}"/>, <see cref="IReactiveAddonLoader{T}"/> and
	/// <see cref="IDiagnosticAddonFactory{T}"/> per addon CLR type.
	/// </summary>
	/// <remarks>
	/// The app-level services are a separate set from the chat-level ones: app services are populated
	/// by <see cref="Management.AppAddonManager"/> with app pack paths, while every chat scope owns
	/// its own scoped services (see <see cref="AddonChatServicesConfigurator"/>) populated by
	/// <see cref="Management.ChatAddonManager"/> with chat-specific pack/extra paths.
	/// </remarks>
	[ServiceConfigurator(ServiceScope.App)]
	public class AddonAppServicesConfigurator : ServiceConfigurator
	{
		public override void Configure(IServiceCollection services)
		{
			var descriptorTypes = ReflectionUtility.GetTypesWithAttribute<IAddonTypeDescriptor, AddonTypeDescriptorAttribute>();
			foreach (var descriptorType in descriptorTypes)
			{
				var descriptor = descriptorType.Type.Instantiate<IAddonTypeDescriptor>();
				services.AddSingleton(typeof(IAddonTypeDescriptor), descriptor);
				AddonServiceRegistration.Register(services, descriptor.ClrType, isAppScope: true);
			}
		}
	}

	/// <summary>
	/// Registers closed generic addon services in the chat scope as SCOPED services: every chat scope
	/// (each chat creates its own DI scope) gets its own loader/accessor/factory instances, populated
	/// with chat-specific addon sources.
	/// </summary>
	/// <remarks>
	/// These services are intentionally registered as CLOSED generics instead of open generic
	/// [Service]/[ChatService] attributes: <see cref="ServiceRegistry.AddAppServices"/> copies open
	/// generic registrations into the chat builder as separate singleton instances, which duplicated
	/// the scoped chat loaders and broke CollectionChanged subscriptions in <see cref="AddonAccessor{T}"/>
	/// (subscriptions were made on different loader instances than the ones being populated).
	/// </remarks>
	[ServiceConfigurator(ServiceScope.Chat)]
	public class AddonChatServicesConfigurator : ServiceConfigurator
	{
		public override void Configure(IServiceCollection services)
		{
			foreach (var descriptor in ServiceRegistry.Provider.GetServices<IAddonTypeDescriptor>())
			{
				services.AddSingleton(typeof(IAddonTypeDescriptor), descriptor);
				AddonServiceRegistration.Register(services, descriptor.ClrType, isAppScope: false);
			}
		}
	}

	internal static class AddonServiceRegistration
	{
		public static void Register(IServiceCollection services, Type addonType, bool isAppScope)
		{
			Register(typeof(IAddonAccessor<>), typeof(AddonAccessor<>));
			Register(typeof(IReactiveAddonLoader<>), typeof(AddonFileCachedLoader<>));
			Register(typeof(IDiagnosticAddonFactory<>), typeof(DiagnosticAddonFactory<>));

			void Register(Type openServiceType, Type openImplementationType)
			{
				var serviceType = openServiceType.MakeGenericType(addonType);
				var implementationType = openImplementationType.MakeGenericType(addonType);

				if (isAppScope)
					services.AddSingleton(serviceType, implementationType);
				else
					services.AddScoped(serviceType, implementationType);
			}
		}
	}
}
