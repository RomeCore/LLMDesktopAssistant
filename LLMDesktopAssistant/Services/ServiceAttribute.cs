namespace LLMDesktopAssistant.Services
{
	/// <summary>
	/// Represents an attribute that can be used to mark a class as a service to be registered in <see cref="ServiceRegistry"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class ServiceAttribute(Type? serviceType = null) : Attribute
	{
		/// <summary>
		/// Gets or sets the type of the service. If not specified, the class itself will be used as the service type.
		/// </summary>
		public Type? ServiceType { get; } = serviceType;
	}
}