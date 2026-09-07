namespace LLMDesktopAssistant.Addons
{
	public enum AddonSource
	{
		Unknown,

		/// <summary>
		/// The addon was loaded from template (LLT, Handlebars, Fluid, etc.).
		/// </summary>
		Template,

		/// <summary>
		/// The addon was loaded from a pack (implicit or explicit).
		/// </summary>
		Pack
	}
}
