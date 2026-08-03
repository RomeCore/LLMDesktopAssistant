namespace LLMDesktopAssistant.Utils.Json
{
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public sealed class JsonDerivedAttribute(Type baseType, string discriminator) : Attribute
	{
		/// <summary>
		/// The type of the base class.
		/// </summary>
		public Type BaseType { get; } = baseType;

		/// <summary>
		/// The key used to discriminate between different derived types in JSON.
		/// </summary>
		public string Discriminator { get; } = discriminator;
	}
}
