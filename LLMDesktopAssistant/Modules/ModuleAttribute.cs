namespace LLMDesktopAssistant.Modules
{
	/// <summary>
	/// Represents an attribute that can be used to mark a class as a module.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class ModuleAttribute : Attribute
	{
		/// <summary>
		/// Gets or sets the order in which the module should be initialized.
		/// </summary>
		public int Order { get; set; }
	}
}