namespace LLMDesktopAssistant.Services.Instances
{
	public interface IApplicationViewActivationEvents
	{
		/// <summary>
		/// The event that is invoked when main window or main view of application gains focus.
		/// </summary>
		event Action? Activated;
	}
}
