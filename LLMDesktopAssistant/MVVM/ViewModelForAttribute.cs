namespace LLMDesktopAssistant.MVVM
{
	/// <summary>
	/// Specifies the view associated with a ViewModel.
	/// Apply this attribute to a ViewModel class to indicate which view it is associated with.
	/// </summary>
	/// <param name="targetView">The type of the view to bind this ViewModel to.</param>
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class ViewModelForAttribute(Type targetView) : Attribute
	{
		public Type TargetView { get; } = targetView;
	}
}