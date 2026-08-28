using LLTSharp;
using LLTSharp.Metadata;

namespace LLMDesktopAssistant.Prompting.Parameterization
{
	public class ParameterSchemaTemplateMetadataFactory : MetadataFactory
	{
		public override bool TryCreateMetadata(string key, TemplateDataAccessor value, out IMetadata metadata)
		{
			if (key is "params_schema")
			{
				metadata = new ParameterSchemaTemplateMetadata { Value = value };
				return true;
			}
			metadata = null!;
			return false;
		}
	}
}
