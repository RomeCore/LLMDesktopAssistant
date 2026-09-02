using System.Collections.Immutable;
using LLMDesktopAssistant.StructuredValues.Const;
using LLMDesktopAssistant.StructuredValues.Reactive;

namespace LLMDesktopAssistant.StructuredValues.Converters
{
	/// <summary>
	/// Provides conversions between immutable (<see cref="ConstNodeValue"/>) and mutable
	/// (<see cref="ReactiveNodeValue"/>) structured node values.
	/// </summary>
	public static class StructuredValueConverter
	{
		/// <summary>
		/// Converts the given structured node value to an immutable one (<see cref="ConstNodeValue"/>).
		/// Values that are already immutable are returned as-is.
		/// </summary>
		/// <param name="value">The value to convert, or <see langword="null"/>.</param>
		/// <returns>
		/// The immutable node value, or <see langword="null"/> if <paramref name="value"/> is <see langword="null"/>.
		/// </returns>
		public static ConstNodeValue? ToConstNodeValue(this INodeValue? value)
		{
			return value is null ? null : ConvertToConst(value);
		}

		/// <summary>
		/// Converts the given structured node value to a mutable (reactive) one (<see cref="ReactiveNodeValue"/>).
		/// Values that are already reactive are returned as-is, unless <paramref name="clone"/> is
		/// <see langword="true"/>, in which case a deep copy is created.
		/// </summary>
		/// <param name="value">The value to convert, or <see langword="null"/>.</param>
		/// <param name="clone">
		/// Whether to create a deep copy even if <paramref name="value"/> is already reactive.
		/// </param>
		/// <returns>
		/// The reactive node value, or <see langword="null"/> if <paramref name="value"/> is <see langword="null"/>.
		/// </returns>
		public static ReactiveNodeValue? ToReactiveNodeValue(this INodeValue? value, bool clone = false)
		{
			return value is null ? null : ConvertToReactive(value, clone);
		}

		private static ConstNodeValue ConvertToConst(INodeValue value) => value switch
		{
			ConstNodeValue constValue => constValue,
			INodeNullValue or null => ConstNodeNullValue.Instance,
			INodeBooleanValue b => new ConstNodeBooleanValue { Value = b.Value },
			INodeNumberValue n => new ConstNodeNumberValue { Value = n.Value },
			INodeStringValue s => new ConstNodeStringValue { Value = s.Value },
			INodeArrayValue a => new ConstNodeArrayValue { Items = a.Items.Select(ConvertToConst).ToImmutableList() },
			INodeDictionaryValue d => new ConstNodeDictionaryValue
			{
				Items = d.Items.ToImmutableDictionary(kvp => kvp.Key, kvp => ConvertToConst(kvp.Value))
			},
			_ => throw new ArgumentException($"Unsupported structured node value type '{value.GetType().FullName}'.", nameof(value))
		};

		private static ReactiveNodeValue ConvertToReactive(INodeValue value, bool clone)
		{
			switch (value)
			{
				case ReactiveNodeValue reactive when !clone:
					return reactive;
				case INodeNullValue:
				case null:
					return new ReactiveNodeNullValue();
				case INodeBooleanValue b:
					return new ReactiveNodeBooleanValue { Value = b.Value };
				case INodeNumberValue n:
					return new ReactiveNodeNumberValue { Value = n.Value };
				case INodeStringValue s:
					return new ReactiveNodeStringValue { Value = s.Value };
				case INodeArrayValue a:
				{
					var array = new ReactiveNodeArrayValue();
					foreach (var item in a.Items)
						array.Items.Add(ConvertToReactive(item, clone));
					return array;
				}
				case INodeDictionaryValue d:
				{
					var dictionary = new ReactiveNodeDictionaryValue();
					foreach (var kvp in d.Items)
						dictionary.Items.Add(kvp.Key, ConvertToReactive(kvp.Value, clone));
					return dictionary;
				}
				default:
					throw new ArgumentException($"Unsupported structured node value type '{value.GetType().FullName}'.", nameof(value));
			}
		}
	}
}
