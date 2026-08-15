using LiteDB;
using LLMDesktopAssistant.Localization;

namespace LLMDesktopAssistant.Services.Instances
{
	[Service]
	public class LiteDB_BSON_SerializerConfig
	{
		public LiteDB_BSON_SerializerConfig()
		{
			BsonMapper.Global.RegisterType<LocaleKeyBase>(key =>
			{
				var doc = new BsonDocument();
				switch (key)
				{
					case ConstLocaleKey:
						doc["type"] = "const";
						break;
					case LocaleKey:
					default:
						doc["type"] = "default";
						break;
				}
				doc["key"] = key.Key;
				return doc;
			}, bson =>
			{
				if (bson.IsNull)
					return null!;
				var doc = bson.AsDocument;
				var type = doc["type"].AsString;
				var key = doc["key"].AsString;
				switch (type)
				{
					case "const":
						return new ConstLocaleKey(key);
					default:
						return LocaleKey.GetOrCreate(key);
				}
			});
		}
	}
}
