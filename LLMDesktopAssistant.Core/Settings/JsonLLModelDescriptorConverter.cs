using System.Text.Json;
using System.Text.Json.Serialization;
using LLMDesktopAssistant.Core.Modules;
using LLMDesktopAssistant.Core.Modules.Instances;
using RCLargeLanguageModels.Clients;

namespace LLMDesktopAssistant.Core.Settings
{
	public class JsonLLModelDescriptorConverter : JsonConverter<LLModelDescriptorTracked>
	{
		public override LLModelDescriptorTracked? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var str = reader.GetString();
			if (string.IsNullOrEmpty(str))
				return null;

			var list = ModuleManager.Get<LLModelListModule>();
			return list.Registry.GetModel(str);
		}

		public override void Write(Utf8JsonWriter writer, LLModelDescriptorTracked value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(value?.FullName);
		}
	}
}