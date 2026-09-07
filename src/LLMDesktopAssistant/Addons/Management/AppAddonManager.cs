using System.Collections.Concurrent;
using System.Reflection;
using LLMDesktopAssistant.Addons.Loading;
using LLMDesktopAssistant.Services;

namespace LLMDesktopAssistant.Addons.Management
{
	[Service(typeof(IAppAddonManager))]
	public class AppAddonManager(
		IAppAddonPackLocator appPackLocator,
		IEnumerable<IAddonTypeDescriptor> addonTypeDescriptors,
		IServiceProvider services
	) : AddonManagerBase, IAppAddonManager
	{
		private readonly ConcurrentDictionary<Type, MethodInfo> _genericLoadMethods = [];

		protected override void ReloadCore()
		{
			var appPacks = appPackLocator.GetEffectivePacks();
			var packPaths = appPacks.Select(p => new AddonPathInfo(p.Path, null, p)).ToArray();

			foreach (var descriptor in addonTypeDescriptors)
			{
				LoadAddonsForType(descriptor.ClrType, new AddonFileLocatorConfiguration
				{
					PackPaths = packPaths
				});
			}
		}

		private void LoadAddonsForType(Type addonType, AddonFileLocatorConfiguration config)
		{
			var method = _genericLoadMethods.GetOrAdd(addonType, type =>
				GetType().GetMethod(nameof(LoadAddons), BindingFlags.Instance | BindingFlags.NonPublic)!
					.MakeGenericMethod(type));

			method.Invoke(this, [config]);
		}

		private void LoadAddons<T>(AddonFileLocatorConfiguration config)
		{
			var files = services.GetRequiredService<IAddonFileLocator<T>>().LocateFiles(config).ToArray();
			services.GetRequiredService<IReactiveAddonLoader<T>>().Reload(files);
		}
	}
}
