using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace LLMDesktopAssistant.StructuredValues
{
	public static class NodeValueExtensions
	{
		extension(INodeValue? value)
		{
			public string? AsString()
			{
				if (value is null or INodeNullValue)
					return null;

				if (value is INodeStringValue stringValue)
					return stringValue.Value;

				if (value is INodeNumberValue numberValue)
					return numberValue.Value.ToString(CultureInfo.InvariantCulture);

				if (value is INodeBooleanValue booleanValue)
					return booleanValue.Value.ToString();

				return null;
			}

			public double? AsDouble()
			{
				if (value is null or INodeNullValue)
					return null;

				if (value is INodeNumberValue numberValue)
					return numberValue.Value;

				if (value is INodeStringValue stringValue && double.TryParse(stringValue.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
					return result;

				return null;
			}

			public IEnumerable<INodeValue> EnumerateValues()
			{
				if (value is INodeArrayValue arrayValue)
					return arrayValue.Items;

				if (value is INodeDictionaryValue dictionaryValue)
					return dictionaryValue.Items.Values;

				return [];
			}

			public bool TryGetValue(string key, [NotNullWhen(true)] out INodeValue? result)
			{
				if (value is INodeDictionaryValue dictionaryValue && dictionaryValue.Items.TryGetValue(key, out result))
					return true;
				result = null;
				return false;
			}

			public bool TryGetValue<TNode>(string key, [NotNullWhen(true)] out TNode? result)
				where TNode : INodeValue
			{
				if (value is INodeDictionaryValue dictionaryValue &&
					dictionaryValue.Items.TryGetValue(key, out var _result) && _result is TNode node)
				{
					result = node;
					return true;
				}
				result = default;
				return false;
			}
		}
	}
}
