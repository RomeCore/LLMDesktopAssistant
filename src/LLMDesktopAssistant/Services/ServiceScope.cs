namespace LLMDesktopAssistant.Services
{
	public enum ServiceScope
	{
		/// <summary>
		/// Service is scoped to the entire application.
		/// </summary>
		App,

		/// <summary>
		/// Service is scoped to the chat.
		/// </summary>
		Chat,

		/// <summary>
		/// Service is scoped to the Blazor WebUI page
		/// </summary>
		WebUI
	}
}