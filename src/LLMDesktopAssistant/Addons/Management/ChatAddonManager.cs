using System.Collections.Concurrent;
using System.Reflection;
using LLMDesktopAssistant.Addons.Loading;
using LLMDesktopAssistant.LLM.Services;
using Serilog;

namespace LLMDesktopAssistant.Addons.Management
{
	[ChatService(typeof(IChatAddonManager))]
	public class ChatAddonManager(
		IChatAddonPackLocator chatPackLocator,
		IChatSettingsService chatSettings,
		IEnumerable<IAddonTypeDescriptor> addonTypeDescriptors,
		IServiceProvider services
	) : AddonManagerBase, IChatAddonManager
	{
		private readonly ConcurrentDictionary<Type, MethodInfo> _genericLoadMethods = [];

		protected override void ReloadCore()
		{
			var chatPacks = chatPackLocator.GetEffectivePacks();
			var packPaths = chatPacks.Select(p => new AddonPathInfo(p.Path, null, p)).ToArray();

			var allAdditionalSources = chatSettings.Settings.Addons.GetEffectiveAddonSources();
			foreach (var descriptor in addonTypeDescriptors)
			{
				var additionalSources = allAdditionalSources.GetOrAdd(descriptor.Type, type => new());
				var locatorConfig = new AddonFileLocatorConfiguration
				{
					PackPaths = packPaths,
					FolderPaths = additionalSources.AdditionalDirectories.Select(d => new AddonPathInfo(d)).ToArray(),
					AddonFiles = additionalSources.AdditionalFiles.Select(f => new AddonPathInfo(f)).ToArray(),
				};

				try
				{
					LoadAddonsForType(descriptor.ClrType, locatorConfig);
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Failed to load addons for type {AddonType}: {Error}", descriptor.ClrType, ex);
				}
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
