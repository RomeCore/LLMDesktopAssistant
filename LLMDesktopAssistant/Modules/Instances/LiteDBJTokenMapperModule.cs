using LiteDB;
using Newtonsoft.Json.Linq;

namespace LLMDesktopAssistant.Modules.Instances
{
	[Module(Order = int.MinValue)]
	public class LiteDBJTokenMapperModule : Module
	{
		public LiteDBJTokenMapperModule()
		{
			var mapper = BsonMapper.Global;

			mapper.RegisterType(
				SerializeJToken,
				DeserializeJToken);

			mapper.RegisterType(
				SerializeJArray,
				DeserializeJArray);

			mapper.RegisterType(
				SerializeJObject,
				DeserializeJObject);

			mapper.RegisterType(
				SerializeJValue,
				DeserializeJValue);
		}

		private static BsonValue SerializeJToken(JToken token)
		{
			if (token == null)
				return BsonValue.Null;

			switch (token.Type)
			{
				case JTokenType.Object:
					return SerializeJObject((JObject)token);

				case JTokenType.Array:
					return SerializeJArray((JArray)token);

				case JTokenType.String:
				case JTokenType.Integer:
				case JTokenType.Float:
				case JTokenType.Boolean:
				case JTokenType.Date:
				case JTokenType.Null:
				case JTokenType.Uri:
				case JTokenType.Guid:
					return SerializeJValue((JValue)token);

				default:
					return new BsonValue(token.ToString());
			}
		}

		private static JToken DeserializeJToken(BsonValue value)
		{
			if (value.IsNull)
				return JValue.CreateNull();

			if (value.IsDocument)
				return DeserializeJObject(value.AsDocument);

			if (value.IsArray)
				return DeserializeJArray(value.AsArray);

			return DeserializeJValue(value);
		}

		private static BsonValue SerializeJArray(JArray array)
		{
			var bsonArray = new BsonArray();

			foreach (var item in array)
			{
				bsonArray.Add(SerializeJToken(item));
			}

			return bsonArray;
		}

		private static JArray DeserializeJArray(BsonValue value)
		{
			if (!value.IsArray)
				throw new ArgumentException("Value is not a BSON array");

			var jArray = new JArray();
			var bsonArray = value.AsArray;

			foreach (var item in bsonArray)
			{
				jArray.Add(DeserializeJToken(item));
			}

			return jArray;
		}

		private static BsonValue SerializeJObject(JObject obj)
		{
			var doc = new BsonDocument();

			foreach (var prop in obj.Properties())
			{
				doc[prop.Name] = SerializeJToken(prop.Value);
			}

			doc["_type"] = "JObject";

			return doc;
		}

		private static JObject DeserializeJObject(BsonValue value)
		{
			if (!value.IsDocument)
				throw new ArgumentException("Value is not a BSON document");

			var jObject = new JObject();
			var doc = value.AsDocument;

			foreach (var key in doc.Keys)
			{
				if (key == "_type")
					continue;

				jObject[key] = DeserializeJToken(doc[key]);
			}

			return jObject;
		}

		private static BsonValue SerializeJValue(JValue value)
		{
			switch (value.Type)
			{
				case JTokenType.String:
					return new BsonValue(value.Value<string>());

				case JTokenType.Integer:
					return new BsonValue(value.Value<long>());

				case JTokenType.Float:
					return new BsonValue(value.Value<double>());

				case JTokenType.Boolean:
					return new BsonValue(value.Value<bool>());

				case JTokenType.Date:
					return new BsonValue(value.Value<DateTime>());

				case JTokenType.Null:
					return BsonValue.Null;

				case JTokenType.Uri:
					return new BsonValue(value.Value<Uri>()!.ToString());

				case JTokenType.Guid:
					return new BsonValue(value.Value<Guid>().ToString());

				default:
					var objValue = value.Value;
					if (objValue != null)
					{
						return new BsonValue(objValue.ToString());
					}
					return BsonValue.Null;
			}
		}

		private static JValue DeserializeJValue(BsonValue value)
		{
			if (value.IsNull)
				return JValue.CreateNull();

			if (value.IsString)
				return new JValue(value.AsString);

			if (value.IsInt32)
				return new JValue(value.AsInt32);

			if (value.IsInt64)
				return new JValue(value.AsInt64);

			if (value.IsDouble)
				return new JValue(value.AsDouble);

			if (value.IsDecimal)
				return new JValue(value.AsDecimal);

			if (value.IsBoolean)
				return new JValue(value.AsBoolean);

			if (value.IsDateTime)
				return new JValue(value.AsDateTime);

			if (value.IsObjectId)
				return new JValue(value.AsObjectId.ToString());

			if (value.IsGuid)
				return new JValue(value.AsGuid);

			return new JValue(value.RawValue?.ToString());
		}
	}
}