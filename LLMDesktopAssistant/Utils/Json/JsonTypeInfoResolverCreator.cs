using System.Text.Json.Serialization.Metadata;

namespace LLMDesktopAssistant.Utils.Json
{
	public static class JsonTypeInfoResolverCreator
	{
		private static readonly ImmutableDictionary<Type, ImmutableDictionary<string, Type>> _types;

		static JsonTypeInfoResolverCreator()
		{
			var types = new Dictionary<Type, Dictionary<string, Type>>();

			foreach (var derivedType in ReflectionUtility.GetTypesWithAttribute<JsonDerivedAttribute>())
			{
				var baseType = derivedType.Attribute.BaseType;
				var discriminator = derivedType.Attribute.Discriminator;

				if (!types.TryGetValue(baseType, out var discriminators))
					types[baseType] = discriminators = [];

				discriminators[discriminator] = derivedType.Type;
			}

			_types = types.ToImmutableDictionary(k => k.Key, v => v.Value.ToImmutableDictionary());
		}

		public static DefaultJsonTypeInfoResolver Create()
		{
			var resolver = new DefaultJsonTypeInfoResolver();

			resolver.Modifiers.Add(type =>
			{
				if (_types.TryGetValue(type.Type, out var discriminators))
				{
					type.PolymorphismOptions ??= new();
					foreach (var discriminator in discriminators)
						type.PolymorphismOptions.DerivedTypes.Add(new JsonDerivedType(discriminator.Value, discriminator.Key));
				}
			});

			return resolver;
		}
	}
}
