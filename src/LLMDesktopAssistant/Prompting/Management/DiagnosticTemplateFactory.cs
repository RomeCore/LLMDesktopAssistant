using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Addons.Loading;
using LLMDesktopAssistant.Services;
using LLTSharp;

namespace LLMDesktopAssistant.Prompting.Management
{
	[Service(typeof(IDiagnosticAddonFactory<ITemplate>))]
	public class DiagnosticTemplateFactory : IDiagnosticAddonFactory<ITemplate>
	{
		public ITemplate CreateDiagnosticAddon(AddonPathInfo path, AddonDiagnostic diagnostic)
		{
			return new ErrorTemplate
			{
				SourcePath = path.Path,
				Exception = diagnostic.Exceptions.FirstOrDefault() ?? new Exception("No exception found")
			};
		}
	}
}
