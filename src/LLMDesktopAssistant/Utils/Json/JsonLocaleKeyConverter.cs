using System.Text.Json;
using System.Text.Json.Serialization;
using LLMDesktopAssistant.Localization;

namespace LLMDesktopAssistant.Utils.Json
{
	public class JsonLocaleKeyConverter : JsonConverter<LocaleKeyBase>
	{
		public override LocaleKeyBase? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.Null)
				return null;

			if (reader.TokenType != JsonTokenType.StartObject)
				throw new JsonException("LocaleKeyBase must be serialized as an object.");

			string? type = null;
			string? key = null;

			while (reader.Read())
			{
				if (reader.TokenType == JsonTokenType.EndObject)
					break;
				if (reader.TokenType != JsonTokenType.PropertyName)
					throw new JsonException("Expected a property name.");

				var propertyName = reader.GetString();
				reader.Read();
				switch (propertyName)
				{
					case "type":
						type = reader.GetString();
						break;
					case "key":
						key = reader.GetString();
						break;
					default:
						reader.Skip();
						break;
				}
			}

			if (key is null)
				throw new JsonException("LocaleKeyBase object must contain a 'key' property.");

			return type == "const" ? Locale.GetConstKey(key) : Locale.GetKey(key);
		}

		public override void Write(Utf8JsonWriter writer, LocaleKeyBase value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			writer.WriteString("type", value is ConstLocaleKey ? "const" : "default");
			writer.WriteString("key", value.Key);
			writer.WriteEndObject();
		}
	}
}
