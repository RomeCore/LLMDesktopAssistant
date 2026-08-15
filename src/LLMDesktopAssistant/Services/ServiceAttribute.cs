namespace LLMDesktopAssistant.Services
{
	/// <summary>
	/// Represents an attribute that can be used to mark a class as a service to be registered in <see cref="ServiceRegistry"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public sealed class ServiceAttribute(Type? serviceType = null) : Attribute
	{
		/// <summary>
		/// Gets or sets the type of the service. If not specified, the class itself will be used as the service type.
		/// </summary>
		public Type? ServiceType { get; } = serviceType;

		/// <summary>
		/// Gets or sets the registration order. Services with a lower order are registered first, so when multiple
		/// services are registered under the same service type, the one with the highest order wins
		/// (<see cref="IServiceProvider.GetService(Type)"/> returns the last registration).
		/// </summary>
		public int Order { get; init; }
	}
}