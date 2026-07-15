namespace LLMDesktopAssistant.Blazor.Services
{
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class WebUIServiceAttribute(Type? serviceType = null) : Attribute
	{
		public Type? ServiceType { get; } = serviceType;
	}
}
