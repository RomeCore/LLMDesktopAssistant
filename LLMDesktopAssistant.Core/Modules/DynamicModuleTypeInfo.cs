namespace LLMDesktopAssistant.Core.Modules
{
	/// <summary>
	/// Provides info about dynamic module type.
	/// </summary>
	public sealed class DynamicModuleTypeInfo(string id, Type type, int? defaultPriority)
	{
		public string Id { get; } = id;

		public Type Type { get; } = type;

		public int? DefaultPriority { get; } = defaultPriority;
	}
}