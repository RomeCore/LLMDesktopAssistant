using System.Text.Json.Serialization;

namespace LLMDesktopAssistant.Prompting
{
	public class Specialization : PromptBase
	{
		[JsonIgnore]
		public override bool IsBuiltin => PromptRegistry.BuiltinSpecializations.ContainsKey(Id);
	}
}
