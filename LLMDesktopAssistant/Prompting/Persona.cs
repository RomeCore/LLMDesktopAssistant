using System.Text.Json.Serialization;

namespace LLMDesktopAssistant.Prompting
{
	public class Persona : PromptBase
	{
		[JsonIgnore]
		public override bool IsBuiltin => PromptRegistry.BuiltinPersonas.ContainsKey(Id);
	}
}