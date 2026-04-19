namespace LLMDesktopAssistant.Services
{
	/// <summary>
	/// Represents an attribute that can be used to mark a class as a dynamic module.
	/// </summary>
	/// <param name="id">The unique identifier for this module within its category.</param>
	/// <param name="categoryType">The type of the category that this module belongs to.</param>
	[AttributeUsage(AttributeTargets.Class)]
	public class DynamicServiceAttribute(string id, Type categoryType) : Attribute
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
		/// Gets or sets the value
		/// </summary>
		public bool IsDefault
		{
			get => DefaultPriority.HasValue;
			set
			{
				if (value != IsDefault)
				{
					if (value)
						DefaultPriority = int.MinValue;
					else
						DefaultPriority = null;
				}
			}
		}

		/// <summary>
		/// Gets or sets the priority of default module creation. If set to <see langword="null"/>, the module will not try to be created.
		/// </summary>
		public int? DefaultPriority { get; set; } = null;

		/// <summary>
		/// Gets or sets the order in which the module should be initialized.
		/// </summary>
		public int Order { get; set; } = 0;
	}
}