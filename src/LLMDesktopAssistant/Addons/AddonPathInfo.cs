namespace LLMDesktopAssistant.Addons
{
	/// <summary>
	/// Represents information about a addon file, including its path and source.
	/// </summary>
	/// <param name="Path">The full path to the addon file or directory.</param>
	/// <param name="IsShortForm">Indicates if the path is in short form (e.g., 'skills/my-skill/SKILL.md' for full form, 'skills/my-skill.md' for short form).</param>
	/// <param name="SourcePack">The name of the source pack, if applicable.</param>
	public readonly record struct AddonPathInfo(string Path, bool? IsShortForm = null, AddonPackInfo? SourcePack = null);
}
