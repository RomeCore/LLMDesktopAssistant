using System.Text.Json.Serialization;

namespace LLMDesktopAssistant.LLM.Settings
{
	[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
	public abstract class AdditionalEnvironmentSetting : NotifyPropertyChanged
	{
	}
}
