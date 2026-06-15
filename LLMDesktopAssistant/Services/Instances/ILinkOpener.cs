namespace LLMDesktopAssistant.Services.Instances
{
	/// <summary>
	/// Interface for opening URI links.
	/// </summary>
	public interface ILinkOpener
	{
		/// <summary>
		/// Opens a URI link.
		/// </summary>
		/// <param name="uri">The URI to open.</param>
		public void OpenLink(Uri uri);
	}
}
