namespace LLMDesktopAssistant.Addons.Loading
{
	public interface IAddonFileLocatorConfigurationProvider
	{
		/// <summary>
		/// Gets the configuration for the addon file locator.
		/// </summary>
		/// <returns>The configuration for the addon file locator.</returns>
		AddonFileLocatorConfiguration GetConfiguration();
	}
}
