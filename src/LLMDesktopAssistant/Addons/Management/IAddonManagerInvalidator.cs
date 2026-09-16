namespace LLMDesktopAssistant.Addons.Management
{
	public interface IAddonManagerInvalidator
	{
		/// <summary>
		/// Reloads the managers regardless of their current state.
		/// </summary>
		void Reload(AddonKind kinds);

		/// <summary>
		/// Invalidates the managers if the specified path potentially touches addon files.
		/// </summary>
		void Invalidate(AddonKind kinds);

		/// <summary>
		/// Reloads the managers if they are currently invalid.
		/// </summary>
		void ReloadIfInvalid(AddonKind kinds);
	}
}
