namespace LLMDesktopAssistant.Settings
{
	[AttributeUsage(AttributeTargets.Class, Inherited = false)]
	public class SettingsObjectAttribute : Attribute
	{
		public string Id { get; }

		public SettingsObjectAttribute(string id)
		{
			Id = id;
		}
	}
}