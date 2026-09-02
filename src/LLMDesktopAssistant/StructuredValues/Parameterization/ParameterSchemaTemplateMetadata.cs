using LLTSharp.Metadata;

namespace LLMDesktopAssistant.StructuredValues.Parameterization
{
	/// <summary>
	/// LLT metadata that carries the <c>params_schema</c> block converted to an immutable
	/// structured node value (<see cref="Const.ConstNodeValue"/>).
	/// </summary>
	public class ParameterSchemaTemplateMetadata : IMetadata
	{
		public required INodeValue Value { get; init; }
	}
}
