using LLMDesktopAssistant.Addons.MVVM;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Services.Configurators
{
	/// <summary>
	/// Registers every concrete <see cref="IAddonCardFactory{TAddon, TChange}"/> implementation of the
	/// assembly as a chat-scoped service.
	/// </summary>
	/// <remarks>
	/// A card factory lives next to the addon type it describes (the skill factory is a part of the skills
	/// folder, the sub-agent factory will be a part of the sub-agents folder), so the registration is done
	/// by discovery instead of a <c>[ChatService]</c> attribute: the factory must not depend on the
	/// dependency injection layer of the addon types it happens to be placed with.
	/// </remarks>
	[ServiceConfigurator(ServiceScope.Chat)]
	public class AddonCardFactoriesConfigurator : ServiceConfigurator
	{
		public override void Configure(IServiceCollection services)
		{
			var openFactoryType = typeof(IAddonCardFactory<,>);

			foreach (var implementation in ReflectionUtility.ObservedTypes)
			{
				if (implementation.IsInterface || implementation.IsAbstract || implementation.IsGenericTypeDefinition)
					continue;

				foreach (var serviceType in implementation.GetInterfaces())
				{
					if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == openFactoryType)
						services.AddScoped(serviceType, implementation);
				}
			}
		}
	}
}
