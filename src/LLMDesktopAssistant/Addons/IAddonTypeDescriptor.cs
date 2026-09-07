namespace LLMDesktopAssistant.Addons
{
	/// <summary>
	/// The addon type descriptor.
	/// </summary>
	public interface IAddonTypeDescriptor
	{
		/// <summary>
		/// Gets the addon type discriptor.
		/// </summary>
		string Type { get; }

		/// <summary>
		/// Gets the addon CLR type.
		/// </summary>
		Type ClrType { get; }
	}
}
