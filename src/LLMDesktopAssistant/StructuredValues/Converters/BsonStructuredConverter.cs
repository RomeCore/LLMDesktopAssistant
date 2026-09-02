using LiteDB;
using LLMDesktopAssistant.StructuredValues.Const;
using LLMDesktopAssistant.StructuredValues.Reactive;

namespace LLMDesktopAssistant.StructuredValues.Converters
{
	/// <summary>
	/// Provides conversions between structured node values (<see cref="INodeValue"/>) and
	/// LiteDB <see cref="BsonValue"/> trees.
	/// </summary>
	public static class BsonStructuredConverter
	{
		/// <summary>
		/// Converts the given structured node value to a <see cref="BsonValue"/>.
		/// </summary>
		/// <param name="value">The value to convert, or <see langword="null"/>.</param>
		/// <returns>The BSON value (never <see langword="null"/>).</returns>
		public static BsonValue ToBsonValue(this INodeValue? value)
		{
			return value is null ? BsonValue.Null : ConvertToBson(value);
		}

		/// <summary>
		/// Converts the given BSON value to an immutable structured node value (<see cref="ConstNodeValue"/>).
		/// </summary>
		/// <param name="value">The value to convert, or <see langword="null"/>.</param>
		/// <returns>
		/// The immutable node value, or <see langword="null"/> if <paramref name="value"/> is <see langword="null"/>.
		/// </returns>
		public static ConstNodeValue? ToConstNodeValue(this BsonValue? value)
		{
			return value is null ? null : ConvertToConst(value);
		}

		/// <summary>
		/// Converts the given BSON value to a mutable (reactive) structured node value (<see cref="ReactiveNodeValue"/>).
		/// </summary>
		/// <param name="value">The value to convert, or <see langword="null"/>.</param>
		/// <returns>
		/// The reactive node value, or <see langword="null"/> if <paramref name="value"/> is <see langword="null"/>.
		/// </returns>
		public static ReactiveNodeValue? ToReactiveNodeValue(this BsonValue? value)
		{
			return value is null ? null : ConvertToReactive(value);
		}

		private static BsonValue ConvertToBson(INodeValue value) => value switch
		{
			INodeNullValue or null => BsonValue.Null,
			INodeBooleanValue b => new BsonValue(b.Value),
			INodeNumberValue n => new BsonValue(n.Value),
			INodeStringValue s => s.Value is null ? BsonValue.Null : new BsonValue(s.Value),
			INodeArrayValue a => ConvertArrayToBson(a.Items),
			INodeDictionaryValue d => ConvertDictionaryToBson(d.Items),
			_ => throw new ArgumentException($"Unsupported structured node value type '{value.GetType().FullName}'.", nameof(value))
		};

		private static BsonArray ConvertArrayToBson(IReadOnlyList<INodeValue> items)
		{
			var array = new BsonArray();
			foreach (var item in items)
				array.Add(ConvertToBson(item));
			return array;
		}

		private static BsonDocument ConvertDictionaryToBson(IReadOnlyDictionary<string, INodeValue> items)
		{
			var doc = new BsonDocument();
			foreach (var kvp in items)
				doc[kvp.Key] = ConvertToBson(kvp.Value);
			return doc;
		}

		private static ConstNodeValue ConvertToConst(BsonValue value)
		{
			if (value.IsNull)
				return ConstNodeNullValue.Instance;
			if (value.IsBoolean)
				return new ConstNodeBooleanValue { Value = value.AsBoolean };
			if (value.IsString)
				return new ConstNodeStringValue { Value = value.AsString };
			if (value.IsNumber)
				return new ConstNodeNumberValue { Value = value.AsDouble };
			if (value.IsArray)
			{
				return new ConstNodeArrayValue
				{
					Items = value.AsArray.Select(ConvertToConst).ToImmutableList()
				};
			}
			if (value.IsDocument)
			{
				return new ConstNodeDictionaryValue
				{
					Items = value.AsDocument.ToImmutableDictionary(kvp => kvp.Key, kvp => ConvertToConst(kvp.Value))
				};
			}
			throw new ArgumentException($"Unsupported BSON type '{value.Type}'.", nameof(value));
		}

		private static ReactiveNodeValue ConvertToReactive(BsonValue value)
		{
			if (value.IsNull)
				return new ReactiveNodeNullValue();
			if (value.IsBoolean)
				return new ReactiveNodeBooleanValue { Value = value.AsBoolean };
			if (value.IsString)
				return new ReactiveNodeStringValue { Value = value.AsString };
			if (value.IsNumber)
				return new ReactiveNodeNumberValue { Value = value.AsDouble };
			if (value.IsArray)
			{
				var array = new ReactiveNodeArrayValue();
				foreach (var item in value.AsArray)
					array.Items.Add(ConvertToReactive(item));
				return array;
			}
			if (value.IsDocument)
			{
				var dictionary = new ReactiveNodeDictionaryValue();
				foreach (var kvp in value.AsDocument)
					dictionary.Items.Add(kvp.Key, ConvertToReactive(kvp.Value));
				return dictionary;
			}
			throw new ArgumentException($"Unsupported BSON type '{value.Type}'.", nameof(value));
		}
	}
}
