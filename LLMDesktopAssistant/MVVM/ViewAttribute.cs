namespace LLMDesktopAssistant.MVVM
{
	/// <summary>
	/// Specifies the view associated with a ViewModel.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class ViewAttribute(Type targetView) : Attribute
	{
		public Type TargetView { get; } = targetView;
	}
}