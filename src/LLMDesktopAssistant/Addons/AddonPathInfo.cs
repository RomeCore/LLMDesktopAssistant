namespace LLMDesktopAssistant.Addons
{
	/// <summary>
	/// Represents information about a addon file, including its path and source.
	/// </summary>
	/// <param name="Path">The full path to the addon file or directory.</param>
	/// <param name="IsShortForm">Indicates if the path is in short form (e.g., 'skills/my-skill/SKILL.md' for full form, 'skills/my-skill.md' for short form).</param>
	/// <param name="SourcePack">The name of the source pack, if applicable.</param>
	public readonly record struct AddonPathInfo(string Path, bool? IsShortForm = null, AddonPackInfo? SourcePack = null)
	{
		/// <summary>
		/// Gets the fallback addon name derived from the path: the file name without extension for
		/// short form (e.g. 'caveman' for 'skills/caveman.md'), or the folder name for full form
		/// (e.g. 'diagnose' for 'skills/diagnose/SKILL.md').
		/// Used when the addon name cannot be obtained from the file content (frontmatter/heading).
		/// </summary>
		public string FallbackName
		{
			get
			{
				return IsShortForm ?? true
					? System.IO.Path.GetFileNameWithoutExtension(Path)
					: System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(Path))!;
			}
		}
	}
}
