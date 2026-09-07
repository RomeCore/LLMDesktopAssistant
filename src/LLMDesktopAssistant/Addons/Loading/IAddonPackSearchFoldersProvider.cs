namespace LLMDesktopAssistant.Addons.Loading
{
	public interface IAddonPackSearchFoldersProvider
	{
		/// <summary>
		/// Returns a list of folders to search for addon packs.
		/// Used for UI display for users to understand where addon packs are being searched.
		/// The example results: '.agents', '.claude', '.llmassist'.
		/// </summary>
		public IEnumerable<string> GetDefaultSearchFolders();

		/// <summary>
		/// Returns a list of folders to search for addon packs.
		/// The example results: '.agents', '.claude', '.llmassist'.
		/// </summary>
		public IEnumerable<string> GetSearchFolders();
	}
}
