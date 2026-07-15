namespace LLMDesktopAssistant.Blazor.Services
{
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class WebUIServiceAttribute(Type? serviceType = null) : Attribute
	{
		public Type? ServiceType { get; } = serviceType;

		/// <summary>
		/// Determines if the service is scoped to the lifetime of a single request.
		/// </summary>
		public bool IsScoped { get; set; } = true;
	}
}
