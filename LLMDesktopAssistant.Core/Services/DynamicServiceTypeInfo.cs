namespace LLMDesktopAssistant.Services
{
	/// <summary>
	/// Provides info about dynamic module type.
	/// </summary>
	public sealed class DynamicServiceTypeInfo(string id, Type type, int? defaultPriority)
	{
		public string Id { get; } = id;

		public Type Type { get; } = type;

		public int? DefaultPriority { get; } = defaultPriority;
	}
}