namespace LLMDesktopAssistant.Providers
{
	public class ModelItem
	{
		public required string FullName { get; init; }

		public required ModelProviderConfiguration Provider { get; init; }

		public required ModelDescriptor Descriptor { get; init; }
	}
}
