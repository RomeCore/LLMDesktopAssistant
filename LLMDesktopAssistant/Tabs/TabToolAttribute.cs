using MaterialDesignThemes.Wpf;

namespace LLMDesktopAssistant.Tabs
{
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class TabToolAttribute(string id) : Attribute
	{
		public string Id { get; } = id;

		public PackIconKind Icon { get; set; }

		public int Order { get; set; }
	}
}