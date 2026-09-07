namespace LLMDesktopAssistant.Addons.Management
{
	public abstract class AddonManagerBase : IAddonManager
	{
		private bool _invalid = true;

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
		public void ReloadIfInvalid()
		{
			if (_invalid)
				Reload();
		}

		protected abstract void ReloadCore();
	}
}
