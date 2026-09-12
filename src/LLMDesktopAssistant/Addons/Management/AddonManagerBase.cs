namespace LLMDesktopAssistant.Addons.Management
{
	public abstract class AddonManagerBase : IAddonManager
	{
		private volatile bool _invalid = true;

		/// <inheritdoc/>
		public void Reload()
		{
			ReloadCore();
			_invalid = false;
		}

		/// <inheritdoc/>
		public void Invalidate()
		{
			_invalid = true;
		}

		/// <inheritdoc/>
		public bool ReloadIfInvalid()
		{
			if (_invalid)
			{
				Reload();
				return true;
			}
			return false;
		}

		protected abstract void ReloadCore();
	}
}
