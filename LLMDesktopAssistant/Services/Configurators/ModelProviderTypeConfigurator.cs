using LLMDesktopAssistant.Providers;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Services.Configurators
{
	[ServiceConfigurator(ServiceScope.App)]
	public class ModelProviderTypeConfigurator : ServiceConfigurator
	{
		public override void Configure(IServiceCollection services)
		{
			var providerTypes = ReflectionUtility.GetTypesWithAttribute<ModelProviderType, ModelProviderTypeAttribute>().ToList();
			foreach (var providerType in providerTypes)
			{
				services.AddSingleton(typeof(ModelProviderType), providerType.Type);
			}
		}
	}
}