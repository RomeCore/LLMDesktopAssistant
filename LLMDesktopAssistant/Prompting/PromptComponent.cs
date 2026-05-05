using System.Text.Json.Serialization;

namespace LLMDesktopAssistant.Prompting
{
	public class PromptComponent : PromptBase
	{
		[JsonIgnore]
		public override bool IsBuiltin => PromptRegistry.BuiltinComponents.ContainsKey(Id);
	}
}