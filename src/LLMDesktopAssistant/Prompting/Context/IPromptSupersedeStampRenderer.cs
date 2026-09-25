namespace LLMDesktopAssistant.Prompting.Context
{
	/// <summary>
	/// Renders a supersede section stamp into a compact text fragment
	/// that is injected into a system reminder of the conversation.
	/// </summary>
	/// <typeparam name="TStamp">The type of the section stamp.</typeparam>
	public interface IPromptSupersedeStampRenderer<TStamp>
		where TStamp : PromptSupersedeStampBase
	{
		/// <summary>
		/// Renders the stamp into a system reminder fragment.
		/// </summary>
		/// <param name="stamp">The stamp to render.</param>
		/// <returns>The rendered text fragment.</returns>
		string Render(TStamp stamp);
	}
}
