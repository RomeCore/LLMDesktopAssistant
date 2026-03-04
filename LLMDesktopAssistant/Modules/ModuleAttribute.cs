namespace LLMDesktopAssistant.Modules
{
	/// <summary>
	/// Represents an attribute that can be used to mark a class as a module.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class ModuleAttribute : Attribute
	{
		/// <summary>
		/// Gets or sets the order in which the module should be initialized and accessed. <br/>
		/// For example <see cref="int.MinValue"/> means that this module is initialized first
		/// and guaranteed to be and returned by <see cref="ModuleManager.Get{T}"/> method.
		/// </summary>
		public int Order { get; set; } = 0;
	}
}