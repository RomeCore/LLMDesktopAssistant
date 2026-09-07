using LLMDesktopAssistant.Services;

namespace LLMDesktopAssistant.Addons.Loading
{
	[Service(typeof(IDiagnosticAddonFactory<>))]
	public class DiagnosticAddonFactory<TAddon> : IDiagnosticAddonFactory<TAddon>
		where TAddon : AddonBase<TAddon>, new()
	{
		public TAddon CreateDiagnosticAddon(AddonPathInfo path, AddonDiagnostic diagnostic)
		{
			return new TAddon
			{
				Name = path.IsShortForm ?? true ? Path.GetFileName(path.Path) : Path.GetFileName(Path.GetDirectoryName(path.Path))!,
				Description = string.Empty,
				
				Source = AddonSource.Unknown,
				SourcePack = path.SourcePack,

				Diagnostic = diagnostic
			};
		}
	}
}
