namespace LLMDesktopAssistant.Core.MVVM
{
	/// <summary>
	/// Specifies the view model associated with a View.
	/// Apply this attribute to a View (control) and it will automatically map the view model.
	/// </summary>
	/// <param name="targetViewModel">The type of the view model to bind this View to.</param>
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class ViewForAttribute(Type targetViewModel) : Attribute
	{
		public Type TargetViewModel { get; } = targetViewModel;
	}
}