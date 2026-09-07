namespace LLMDesktopAssistant.Addons.Loading
{
	public interface IDiagnosticAddonFactory<out T>
	{
		T CreateDiagnosticAddon(AddonPathInfo path, AddonDiagnostic diagnostic);
	}
}
