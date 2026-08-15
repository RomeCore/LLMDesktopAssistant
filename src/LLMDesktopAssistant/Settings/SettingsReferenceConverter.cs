using System.Text.Json;
using System.Text.Json.Serialization;

namespace LLMDesktopAssistant.Settings
{
	/// <summary>
	/// Creates JSON converters for closed generic types of <see cref="SettingsReference{T}"/>.
	/// </summary>
	public class SettingsReferenceConverter : JsonConverterFactory
	{
		/// <inheritdoc />
		public override bool CanConvert(Type typeToConvert)
		{
			return typeToConvert.IsGenericType
				&& typeToConvert.GetGenericTypeDefinition() == typeof(SettingsReference<>);
		}

		/// <inheritdoc />
		public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
		{
			var elementType = typeToConvert.GetGenericArguments()[0];
			var converterType = typeof(SettingsReferenceJsonConverter<>).MakeGenericType(elementType);
			return (JsonConverter)Activator.CreateInstance(converterType)!;
		}

		private class SettingsReferenceJsonConverter<T> : JsonConverter<SettingsReference<T>>
			where T : SettingsObject, new()
		{
			/// <inheritdoc />
			public override SettingsReference<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				if (reader.TokenType == JsonTokenType.Null)
					return null;

				var id = reader.GetString();
				return string.IsNullOrWhiteSpace(id) ? null : new SettingsReference<T> { Id = id };
			}

			/// <inheritdoc />
			public override void Write(Utf8JsonWriter writer, SettingsReference<T> value, JsonSerializerOptions options)
			{
				writer.WriteStringValue(value.Id);
			}
		}
	}
}