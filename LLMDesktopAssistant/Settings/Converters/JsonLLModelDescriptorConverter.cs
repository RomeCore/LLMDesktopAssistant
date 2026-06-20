using System.Text.Json;
using System.Text.Json.Serialization;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Services.Instances;
using RCLargeLanguageModels.Clients;

namespace LLMDesktopAssistant.Settings.Converters
{
	public class JsonLLModelDescriptorConverter : JsonConverter<LLModelDescriptorTracked>
	{
		public override LLModelDescriptorTracked? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var str = reader.GetString();
			if (string.IsNullOrEmpty(str))
				return null;

			var list = ServiceRegistry.Provider.GetRequiredService<LLModelListService>();
			return list.Registry.GetModel(str);
		}

		public override void Write(Utf8JsonWriter writer, LLModelDescriptorTracked value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(value?.FullName);
		}
	}
}