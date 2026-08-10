using LLMDesktopAssistant.Prompting;

namespace LLMDesktopAssistant.Services.Configurators
{
	[ServiceConfigurator(ServiceScope.App)]
	public class TemplateLibraryConfigurator : ServiceConfigurator
	{
		public override void Configure(IServiceCollection services)
		{
			services.AddSingleton(s => s.GetRequiredService<IPromptRegistry>().SharedLibrary);
		}
	}
}