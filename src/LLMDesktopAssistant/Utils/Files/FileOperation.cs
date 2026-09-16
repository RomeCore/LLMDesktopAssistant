namespace LLMDesktopAssistant.Utils.Files
{
	/// <summary>
	/// Describes what happens with a file system path in the moment its addon impact is being detected
	/// (see <c>LLMDesktopAssistant.Addons.Loading.IAddonPathImpactDetector</c>).
	/// </summary>
	public enum FileOperation
	{
		/// <summary>
		/// The path is being created. Directories between the existing ancestor directory (when provided)
		/// and the path can be created implicitly by the operation, which can change the addon pack topology.
		/// </summary>
		Create,

		/// <summary>
		/// The content of an existing path is being modified. No directories appear or disappear.
		/// </summary>
		Edit,

		/// <summary>
		/// The existing path is being removed. Directories on the path can disappear, but nothing is created.
		/// </summary>
		Delete
	}
}
