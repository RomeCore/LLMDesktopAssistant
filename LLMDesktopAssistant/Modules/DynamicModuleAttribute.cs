namespace LLMDesktopAssistant.Modules
{
	/// <summary>
	/// Represents an attribute that can be used to mark a class as a dynamic module.
	/// </summary>
	/// <param name="id">The unique identifier for this module within its category.</param>
	/// <param name="categoryType">The type of the category that this module belongs to.</param>
	[AttributeUsage(AttributeTargets.Class)]
	public class DynamicModuleAttribute(string id, Type categoryType) : Attribute
	{
		/// <summary>
		/// Gets the unique identifier for this module in the category scope.
		/// </summary>
		public string Id { get; } = id;

		/// <summary>
		/// Gets the type of the category that this module belongs to.
		/// </summary>
		public Type CategoryType { get; } = categoryType;

		/// <summary>
		/// Gets or sets the order in which the module should be initialized.
		/// </summary>
		public int Order { get; set; }
	}
}