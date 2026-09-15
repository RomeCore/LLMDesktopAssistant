namespace LLMDesktopAssistant.Addons.Management
{
	public abstract class AddonManagerBase : IAddonManager
	{
		private volatile AddonKind _invalid = AddonKind.All;

		/// <inheritdoc/>
		public void Reload(AddonKind kinds)
		{
			ReloadCore(kinds);
			_invalid &= ~kinds;
		}

		/// <inheritdoc/>
		public void Invalidate(AddonKind kinds)
		{
			_invalid |= kinds;
		}

		/// <inheritdoc/>
		public bool ReloadIfInvalid(AddonKind kinds)
		{
			var dirty = _invalid & kinds;
			if (dirty == 0)
				return false;
			Reload(dirty);
			return true;
		}

		protected abstract void ReloadCore(AddonKind kinds);
	}
}
