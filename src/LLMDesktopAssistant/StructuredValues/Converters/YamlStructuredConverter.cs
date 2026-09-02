using System.Globalization;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using LLMDesktopAssistant.StructuredValues.Const;
using LLMDesktopAssistant.StructuredValues.Reactive;

namespace LLMDesktopAssistant.StructuredValues.Converters
{
	/// <summary>
	/// Provides conversions between structured node values (<see cref="INodeValue"/>) and
	/// YamlDotNet representation model nodes (<see cref="YamlNode"/>).
	/// </summary>
	/// <remarks>
	/// Plain YAML scalars are interpreted as <see langword="null"/>, booleans or numbers, while
	/// quoted scalars are always treated as strings. When converting strings that look like plain
	/// scalars of other types, they are emitted quoted to preserve their semantics.
	/// </remarks>
	public static class YamlStructuredConverter
	{
		/// <summary>
		/// Converts the given structured node value to a <see cref="YamlNode"/>.
		/// </summary>
		/// <param name="value">The value to convert, or <see langword="null"/>.</param>
		/// <returns>The YAML node (never <see langword="null"/>).</returns>
		public static YamlNode ToYamlNode(this INodeValue? value)
		{
			return value is null ? CreateNullScalar() : ConvertToYaml(value);
		}

		/// <summary>
		/// Converts the given YAML node to an immutable structured node value (<see cref="ConstNodeValue"/>).
		/// </summary>
		/// <param name="node">The node to convert, or <see langword="null"/>.</param>
		/// <returns>
		/// The immutable node value, or <see langword="null"/> if <paramref name="node"/> is <see langword="null"/>.
		/// </returns>
		public static ConstNodeValue? ToConstNodeValue(this YamlNode? node)
		{
			return node is null ? null : ConvertToConst(node);
		}

		/// <summary>
		/// Converts the given YAML node to a mutable (reactive) structured node value (<see cref="ReactiveNodeValue"/>).
		/// </summary>
		/// <param name="node">The node to convert, or <see langword="null"/>.</param>
		/// <returns>
		/// The reactive node value, or <see langword="null"/> if <paramref name="node"/> is <see langword="null"/>.
		/// </returns>
		public static ReactiveNodeValue? ToReactiveNodeValue(this YamlNode? node)
		{
			return node is null ? null : ConvertToReactive(node);
		}

		private static YamlNode ConvertToYaml(INodeValue value) => value switch
		{
			INodeNullValue or null => CreateNullScalar(),
			INodeBooleanValue b => new YamlScalarNode(b.Value ? "true" : "false"),
			INodeNumberValue n => new YamlScalarNode(n.Value.ToString("R", CultureInfo.InvariantCulture)),
			INodeStringValue s => CreateStringScalar(s.Value),
			INodeArrayValue a => ConvertArrayToYaml(a.Items),
			INodeDictionaryValue d => ConvertDictionaryToYaml(d.Items),
			_ => throw new ArgumentException($"Unsupported structured node value type '{value.GetType().FullName}'.", nameof(value))
		};

		private static YamlScalarNode CreateNullScalar()
		{
			return new YamlScalarNode("null");
		}

		private static YamlScalarNode CreateStringScalar(string? text)
		{
			// Null strings are emitted as YAML nulls (YAML has no way to distinguish them from null values).
			if (text is null)
				return CreateNullScalar();

			var scalar = new YamlScalarNode(text);

			// Quote strings that could be interpreted as null, boolean or number when read back,
			// and empty strings (which are null in YAML) to preserve their string semantics.
			if (IsNullText(text) ||
				bool.TryParse(text, out _) ||
				double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _) ||
				text.Length == 0)
			{
				scalar.Style = ScalarStyle.SingleQuoted;
			}

			return scalar;
		}

		private static YamlSequenceNode ConvertArrayToYaml(IReadOnlyList<INodeValue> items)
		{
			var children = items.Select(ConvertToYaml).ToArray();
			return new YamlSequenceNode(children);
		}

		private static YamlMappingNode ConvertDictionaryToYaml(IReadOnlyDictionary<string, INodeValue> items)
		{
			var children = new List<YamlNode>(items.Count * 2);
			foreach (var kvp in items)
			{
				children.Add(new YamlScalarNode(kvp.Key));
				children.Add(ConvertToYaml(kvp.Value));
			}
			return new YamlMappingNode(children.ToArray());
		}

		private static ConstNodeValue ConvertToConst(YamlNode node) => node switch
		{
			YamlScalarNode scalar => ConvertScalarToConst(scalar),
			YamlSequenceNode sequence => new ConstNodeArrayValue
			{
				Items = sequence.Children.Select(ConvertToConst).ToImmutableList()
			},
			YamlMappingNode mapping => new ConstNodeDictionaryValue
			{
				Items = mapping.Children.ToImmutableDictionary(pair => pair.Key.ToString(), pair => ConvertToConst(pair.Value))
			},
			_ => throw new ArgumentException($"Unsupported YAML node type '{node.GetType().FullName}'.", nameof(node))
		};

		private static ReactiveNodeValue ConvertToReactive(YamlNode node)
		{
			switch (node)
			{
				case YamlScalarNode scalar:
					return ConvertScalarToReactive(scalar);
				case YamlSequenceNode sequence:
				{
					var array = new ReactiveNodeArrayValue();
					foreach (var item in sequence.Children)
						array.Items.Add(ConvertToReactive(item));
					return array;
				}
				case YamlMappingNode mapping:
				{
					var dictionary = new ReactiveNodeDictionaryValue();
					foreach (var pair in mapping.Children)
						dictionary.Items.Add(pair.Key.ToString(), ConvertToReactive(pair.Value));
					return dictionary;
				}
				default:
					throw new ArgumentException($"Unsupported YAML node type '{node.GetType().FullName}'.", nameof(node));
			}
		}

		private static ConstNodeValue ConvertScalarToConst(YamlScalarNode scalar)
		{
			var text = scalar.Value;

			// Quoted and literal scalars are always strings.
			if (scalar.Style != ScalarStyle.Plain)
				return new ConstNodeStringValue { Value = text };

			if (IsNullText(text))
				return ConstNodeNullValue.Instance;
			if (bool.TryParse(text, out var boolean))
				return new ConstNodeBooleanValue { Value = boolean };
			if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
				return new ConstNodeNumberValue { Value = number };
			return new ConstNodeStringValue { Value = text };
		}

		private static ReactiveNodeValue ConvertScalarToReactive(YamlScalarNode scalar)
		{
			var text = scalar.Value;

			if (scalar.Style != ScalarStyle.Plain)
				return new ReactiveNodeStringValue { Value = text };

			if (IsNullText(text))
				return new ReactiveNodeNullValue();
			if (bool.TryParse(text, out var boolean))
				return new ReactiveNodeBooleanValue { Value = boolean };
			if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
				return new ReactiveNodeNumberValue { Value = number };
			return new ReactiveNodeStringValue { Value = text };
		}

		private static bool IsNullText(string? text)
		{
			return text is null or "" or "~" or "null" or "Null" or "NULL";
		}
	}
}
