namespace LLMDesktopAssistant.Services
{
	/// <summary>
	/// Represents an attribute that can be used to mark a class as a service to be registered in <see cref="ServiceRegistry"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class ServiceAttribute : Attribute
	{
		/// <summary>
		/// Gets or sets the type of the service. If not specified, the class itself will be used as the service type.
		/// </summary>
		public Type? ServiceType { get; set; }

		/// <summary>
		/// Gets or sets the order in which the service should be registered and accessed. <br/>
		/// For example <see cref="int.MinValue"/> means that this module is initialized first
		/// and guaranteed to be and returned by <see cref="ServiceRegistry.Get{T}"/> method.
		/// </summary>
		public int Order { get; set; } = 0;
	}
}