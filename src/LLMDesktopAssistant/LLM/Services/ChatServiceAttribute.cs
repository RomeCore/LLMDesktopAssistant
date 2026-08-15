namespace LLMDesktopAssistant.LLM.Services
{
	/// <summary>
	/// The attribute to mark a class as a scoped chat service.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public sealed class ChatServiceAttribute(Type? serviceType = null) : Attribute
	{
		/// <summary>
		/// Gets the type of the chat service.
		/// </summary>
		public Type? ServiceType { get; } = serviceType;
	}
}