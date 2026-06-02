namespace LLMDesktopAssistant.Services
{
	/// <summary>
	/// Attribute used to mark a class as a service configurator.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class ServiceConfiguratorAttribute(ServiceScope scope = ServiceScope.App) : Attribute
	{
		/// <summary>
		/// Gets the service scope.
		/// </summary>
		public ServiceScope Scope { get; } = scope;
	}
}