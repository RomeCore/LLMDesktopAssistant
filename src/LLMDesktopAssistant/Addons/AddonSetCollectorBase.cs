using LLMDesktopAssistant.Agents;

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

		protected virtual IEnumerable<TAddon> GetAdditionalAddons()
		{
			return [];
		}

		protected virtual void ApplyChange(TAddon target, TChange change)
		{
		}

		public IEnumerable<TAddon> GetAvailableAddons()
		{
			List<TAddon> addons = [];
			addons.AddRange(_accessor.Addons);
			addons.AddRange(GetAdditionalAddons());

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

		public abstract IEnumerable<TAddon> GetAddonsForAgent(ChatAgentDescriptor agent);

		protected IEnumerable<TAddon> GetAddonsWithChanges(IEnumerable<TChange> changes, bool enabledByDefault)
		{
			var addons = GetAvailableAddons();
			var changesMap = changes.ToDictionary(c => c.Name);
			var result = new List<TAddon>();

			foreach (var addon in addons)
			{
				if (addon.Diagnostic?.IsFatal == true)
					continue;

				if (changesMap.TryGetValue(addon.Name, out var change))
				{
					if (change.Enabled ?? addon.Enabled ?? enabledByDefault)
					{
						var clone = addon.Clone();
						ApplyChange(clone, change);
						clone.Enabled = true;
						clone.Change = change;
						clone.Freeze();
						result.Add(clone);
					}
				}
				else
				{
					if (addon.Enabled ?? enabledByDefault)
						result.Add(addon);
				}
			}

			return result;
		}
	}
}
