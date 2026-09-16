using LLMDesktopAssistant.Agents;
using Serilog;

namespace LLMDesktopAssistant.Addons
{
	public abstract class AddonSetCollectorBase<TAddon, TChange> : IAddonSetCollector<TAddon>
		where TAddon : AddonChangedBase<TAddon, TChange>, new()
		where TChange : AddonChangeBase
	{
		private readonly IServiceProvider _services;
		private readonly IAddonAccessor<TAddon> _accessor;

		public AddonSetCollectorBase(IServiceProvider services)
		{
			_services = services;
			_accessor = _services.GetRequiredService<IAddonAccessor<TAddon>>();
		}

		protected virtual bool AdditionalGoingFirst => false;

		protected virtual IEnumerable<TAddon> GetAdditionalAddons()
		{
			return [];
		}

		protected virtual void ApplyChange(TAddon target, TChange change, ChatAgentDescriptor? agent)
		{
		}

		public IEnumerable<TAddon> GetAvailableAddons()
		{
			List<TAddon> addons = [];

			if (AdditionalGoingFirst)
			{
				addons.AddRange(GetAdditionalAddons());
				addons.AddRange(_accessor.Addons);
			}
			else
			{
				addons.AddRange(_accessor.Addons);
				addons.AddRange(GetAdditionalAddons());
			}

			return addons
				.GroupBy(s => s.Name)
				.Select(g =>
				{
					ImmutableList<TAddon>.Builder? overridesBuilder = null;
					TAddon? last = null;
					foreach (var addon in g)
					{
						addon.Freeze();
						if (last is not null)
						{
							overridesBuilder ??= ImmutableList.CreateBuilder<TAddon>();
							overridesBuilder.Add(last);
						}
						last = addon;
					}
					if (overridesBuilder == null)
						return last!;
					last = last!.Clone();
					last.Overrides = overridesBuilder.ToImmutable();
					last.Freeze();
					return last;
				});
		}

		public virtual IEnumerable<TAddon> GetAddonsForChat()
		{
			Log.Warning("GetAddons* not implemented for {0}! Returning all addons. Override if necessary.", GetType());
			return GetAvailableAddons();
		}

		public virtual IEnumerable<TAddon> GetAddonsForAgent(ChatAgentDescriptor agent)
		{
			return GetAddonsForChat();
		}

		protected IEnumerable<TAddon> GetAddonsWithChanges(AddonSetConfigurationBase<TChange> setConfig,
			ChatAgentDescriptor? agent)
		{
			var addons = GetAvailableAddons();
			var result = new List<TAddon>();

			foreach (var addon in addons)
			{
				if (addon.Diagnostic?.IsFatal == true)
					continue;

				if (setConfig.Changes.TryGetValue(addon.Name, out var change))
				{
					if (addon.IsFixed || (change.Enabled ?? addon.Enabled ?? setConfig.EnabledByDefault))
					{
						var clone = addon.Clone();
						ApplyChange(clone, change, agent);
						clone.Enabled = true;
						if (addon.IsFixed)
							clone.Hidden = false;
						else
							clone.Hidden = change.Hidden ?? addon.Hidden ?? setConfig.HiddenByDefault;
						clone.Change = change;
						clone.Freeze();
						result.Add(clone);
					}
				}
				else
				{
					if (addon.IsFixed || (addon.Enabled ?? setConfig.EnabledByDefault))
					{
						var clone = addon.Clone();
						clone.Enabled = true;
						if (addon.IsFixed)
							clone.Hidden = false;
						else
							clone.Hidden = addon.Hidden ?? setConfig.HiddenByDefault;
						clone.Freeze();
						result.Add(clone);
					}
				}
			}

			return result;
		}
	}
}
