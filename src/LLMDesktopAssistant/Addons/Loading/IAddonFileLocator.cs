namespace LLMDesktopAssistant.Addons.Loading
{
	/// <summary>
	/// Locates all addon files for the effective configuration of a specific addon type.
	/// </summary>
	/// <typeparam name="T">The addon type that this locator is responsible for.</typeparam>
	public interface IAddonFileLocator<T>
	{
		/// <summary>
		/// Locates all addon files for effective configuration.
		/// </summary>
		/// <remarks>
		/// Note that locator can return non-existing files and directories (if user explicitly specified them),
		/// which should be handled by the caller (produce 'failed' status for addons with diagnostic info).
		/// </remarks>
		IEnumerable<AddonPathInfo> LocateFiles(AddonFileLocatorConfiguration config);
	}
}
