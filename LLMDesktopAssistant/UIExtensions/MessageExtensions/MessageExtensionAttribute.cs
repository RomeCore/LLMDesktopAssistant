namespace LLMDesktopAssistant.UIExtensions.MessageExtensions
{
	/// <summary>
	/// The attribute for the message extension. This attribute is used to mark a class as a message extension.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class MessageExtensionAttribute : Attribute
	{
		/// <summary>
		/// Defines the targets of the extension.
		/// </summary>
		public MessageExtensionTargets Targets { get; set; } = MessageExtensionTargets.Both;
	}
}
