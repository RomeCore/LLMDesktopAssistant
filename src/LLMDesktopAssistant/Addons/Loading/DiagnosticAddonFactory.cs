namespace LLMDesktopAssistant.Addons.Loading
{
	public class DiagnosticAddonFactory<TAddon> : IDiagnosticAddonFactory<TAddon>
		where TAddon : AddonBase<TAddon>, new()
	{
		public TAddon CreateDiagnosticAddon(AddonPathInfo path, AddonDiagnostic diagnostic)
		{
			return new TAddon
			{
				Name = path.FallbackName,
				Description = string.Empty,
				
				AddonSource = AddonSource.Unknown,
				SourcePack = path.SourcePack,

				Diagnostic = diagnostic
			};
		}
	}
}
