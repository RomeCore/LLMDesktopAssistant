using LLTSharp;
using LLTSharp.Metadata;

namespace LLMDesktopAssistant.Prompting.Parameterization
{
	public class ParameterSchemaTemplateMetadata : IMetadata
	{
		public required TemplateDataAccessor Value { get; init; }
	}
}
