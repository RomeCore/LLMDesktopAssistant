namespace LLMDesktopAssistant.Addons.Loading
{
	public class AddonFileLocatorConfiguration
	{
		/// <summary>
		/// The pack paths where addons can be found.
		/// Pack paths usually includes subdirectories like 'skills' and 'agents'.
		/// Similar directories: '.agents/', '.agents/packs/my-pack/'.
		/// To provide directories like 'skills' and 'agents' directly, use <see cref="FolderPaths"/>
		/// </summary>
		public AddonPathInfo[]? PackPaths { get; set; }

		/// <summary>
		/// The folder paths where addons can be found.
		/// Similar directories: '.agents/skills/' and '.agents/agents/'.
		/// </summary>
		public AddonPathInfo[]? FolderPaths { get; set; }

		/// <summary>
		/// The direct addon files to return from locator.
		/// </summary>
		public AddonPathInfo[]? AddonFiles { get; set; }
	}
}
