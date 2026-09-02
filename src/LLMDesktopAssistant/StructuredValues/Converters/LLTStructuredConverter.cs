using System.Collections.Immutable;
using LLMDesktopAssistant.StructuredValues.Const;
using LLMDesktopAssistant.StructuredValues.Reactive;
using LLTSharp;
using LLTSharp.DataAccessors;

namespace LLMDesktopAssistant.StructuredValues.Converters
{
	/// <summary>
	/// Provides conversions between structured node values (<see cref="INodeValue"/>) and
	/// LLTSharp template data structures.
	/// </summary>
	public static class LLTStructuredConverter
	{
		/// <summary>
		/// Converts the given structured node value to a <see cref="TemplateDataAccessor"/>
		/// that can be used for template rendering.
		/// </summary>
		/// <param name="value">The value to convert, or <see langword="null"/>.</param>
		/// <returns>
		/// The template data accessor, or <see langword="null"/> if <paramref name="value"/> is <see langword="null"/>.
		/// </returns>
		public static TemplateDataAccessor? ToTemplateDataAccessor(this INodeValue? value)
		{
			return value is null ? null : ConvertToTemplate(value);
		}

		/// <summary>
		/// Converts the given LLTSharp template data accessor to an immutable structured node value
		/// (<see cref="ConstNodeValue"/>).
		/// </summary>
		/// <param name="accessor">The accessor to convert, or <see langword="null"/>.</param>
		/// <returns>
		/// The immutable node value, or <see langword="null"/> if <paramref name="accessor"/> is <see langword="null"/>.
		/// </returns>
		public static ConstNodeValue? ToConstNodeValue(this TemplateDataAccessor? accessor)
		{
			return accessor is null ? null : ConvertToConst(accessor);
		}

		/// <summary>
		/// Converts the given LLTSharp template data accessor to a mutable (reactive) structured node value
		/// (<see cref="ReactiveNodeValue"/>).
		/// </summary>
		/// <param name="accessor">The accessor to convert, or <see langword="null"/>.</param>
		/// <returns>
		/// The reactive node value, or <see langword="null"/> if <paramref name="accessor"/> is <see langword="null"/>.
		/// </returns>
		public static ReactiveNodeValue? ToReactiveNodeValue(this TemplateDataAccessor? accessor)
		{
			return accessor is null ? null : ConvertToReactive(accessor);
		}

		private static TemplateDataAccessor ConvertToTemplate(INodeValue value) => value switch
		{
			INodeNullValue or null => TemplateNullAccessor.Instance,
			INodeBooleanValue b => new TemplateBooleanAccessor(b.Value),
			INodeNumberValue n => new TemplateNumberAccessor(n.Value),
			INodeStringValue s => new TemplateStringAccessor(s.Value),
			INodeArrayValue a => new TemplateArrayAccessor(a.Items.Select(ConvertToTemplate)),
			INodeDictionaryValue d => new TemplateDictionaryAccessor(d.Items.ToDictionary(kvp => kvp.Key, kvp => ConvertToTemplate(kvp.Value))),
			_ => throw new ArgumentException($"Unsupported structured node value type '{value.GetType().FullName}'.", nameof(value))
		};

		private static ConstNodeValue ConvertToConst(TemplateDataAccessor accessor) => accessor switch
		{
			TemplateNullAccessor => ConstNodeNullValue.Instance,
			TemplateBooleanAccessor b => new ConstNodeBooleanValue { Value = b.Value },
			TemplateNumberAccessor n => new ConstNodeNumberValue { Value = n.Value },
			TemplateStringAccessor s => new ConstNodeStringValue { Value = s.Value },
			TemplateArrayAccessor a => new ConstNodeArrayValue { Items = a.Select(ConvertToConst).ToImmutableList() },
			TemplateDictionaryAccessor d => new ConstNodeDictionaryValue { Items = d.Dictionary.ToImmutableDictionary(kvp => kvp.Key, kvp => ConvertToConst(kvp.Value)) },
			_ => throw new ArgumentException($"Unsupported LLTSharp template data accessor type '{accessor.GetType().FullName}'.", nameof(accessor))
		};

		private static ReactiveNodeValue ConvertToReactive(TemplateDataAccessor accessor)
		{
			switch (accessor)
			{
				case TemplateNullAccessor:
					return new ReactiveNodeNullValue();
				case TemplateBooleanAccessor b:
					return new ReactiveNodeBooleanValue { Value = b.Value };
				case TemplateNumberAccessor n:
					return new ReactiveNodeNumberValue { Value = n.Value };
				case TemplateStringAccessor s:
					return new ReactiveNodeStringValue { Value = s.Value };
				case TemplateArrayAccessor a:
				{
					var array = new ReactiveNodeArrayValue();
					foreach (var item in a)
						array.Items.Add(ConvertToReactive(item));
					return array;
				}
				case TemplateDictionaryAccessor d:
				{
					var dictionary = new ReactiveNodeDictionaryValue();
					foreach (var kvp in d.Dictionary)
						dictionary.Items.Add(kvp.Key, ConvertToReactive(kvp.Value));
					return dictionary;
				}
				default:
					throw new ArgumentException($"Unsupported LLTSharp template data accessor type '{accessor.GetType().FullName}'.", nameof(accessor));
			}
		}
	}
}
