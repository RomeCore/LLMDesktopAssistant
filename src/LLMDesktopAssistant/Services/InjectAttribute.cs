namespace LLMDesktopAssistant.Services
{
	/// <summary>
	/// Represents an attribute that can be used to mark a property, field or parameter as injectable.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
	public sealed class InjectAttribute : Attribute
	{
	}
}