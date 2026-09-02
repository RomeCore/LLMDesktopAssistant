using System.Collections.Immutable;
using System.Text.Json.Nodes;
using LiteDB;
using LLMDesktopAssistant.StructuredValues;
using LLMDesktopAssistant.StructuredValues.Const;
using LLMDesktopAssistant.StructuredValues.Converters;
using LLMDesktopAssistant.StructuredValues.Reactive;
using YamlDotNet.RepresentationModel;

namespace LLMDesktopAssistant.Tests.StructuredValues;

/// <summary>
/// Round-trip tests for the structured value converters: a complex value is converted to an
/// external representation (JSON / BSON / YAML / Lua / reactive) and back, and the result must
/// be equivalent to the original.
/// </summary>
public class StructuredValueConvertersTests
{
	private static ConstNodeStringValue S(string? value) => new() { Value = value };
	private static ConstNodeNumberValue N(double value) => new() { Value = value };
	private static ConstNodeBooleanValue B(bool value) => new() { Value = value };
	private static ConstNodeArrayValue Arr(params ConstNodeValue[] items) => new() { Items = items.ToImmutableList() };
	private static ConstNodeDictionaryValue Dict(params (string Key, ConstNodeValue Value)[] pairs)
		=> new() { Items = pairs.ToImmutableDictionary(p => p.Key, p => p.Value) };

	private static ConstNodeValue BuildComplex(bool includeEmptyContainers = true, bool includeNulls = true)
	{
		var meta = Dict(
			("version", S("2.1")),
			("text-number", S("123")),
			("text-bool", S("true")),
			("nested", Dict(
				("depth", N(0.001)),
				("name", S("deep")))));

		var items = new List<(string, ConstNodeValue)>
		{
			("title", S("Demo")),
			("count", N(42)),
			("ratio", N(3.5)),
			("enabled", B(true)),
			("disabled", B(false)),
			("meta", meta),
		};

		if (includeNulls)
		{
			items.Insert(5, ("nothing", ConstNodeNullValue.Instance));
			var tags = new List<ConstNodeValue> { S("alpha"), S("beta"), ConstNodeNullValue.Instance };
			items.Add(("tags", Arr(tags.ToArray())));
		}
		else
		{
			items.Add(("tags", Arr(S("alpha"), S("beta"))));
		}

		if (includeEmptyContainers)
		{
			items.Add(("empty_array", Arr()));
			items.Add(("empty_dict", Dict()));
		}

		return Dict(items.ToArray());
	}

	private static JsonNode? ToJson(INodeValue? value) => JsonStructuredConverter.ToJsonNode(value);

	private static void AssertSameValue(INodeValue? expected, INodeValue? actual)
	{
		var expectedJson = ToJson(expected);
		var actualJson = ToJson(actual);
		Assert.True(JsonNode.DeepEquals(expectedJson, actualJson),
			$"Values differ:{Environment.NewLine}Expected: {expectedJson?.ToJsonString()}{Environment.NewLine}Actual:   {actualJson?.ToJsonString()}");
	}

	[Fact]
	public void Json_ComplexValue_RoundTripsThroughJsonNode()
	{
		var value = BuildComplex();

		var node = value.ToJsonNode();
		var back = node?.ToConstNodeValue();

		AssertSameValue(value, back);
	}

	[Fact]
	public void Json_ParsedJson_ConvertsToConstAndReactive()
	{
		var json = """
			{
			  "title": "Demo",
			  "count": 42,
			  "ratio": 3.5,
			  "enabled": true,
			  "disabled": false,
			  "nothing": null,
			  "tags": ["alpha", "beta", null],
			  "meta": {
			    "version": "2.1",
			    "text-number": "123",
			    "nested": { "depth": 0.001, "name": "deep" }
			  },
			  "empty_array": [],
			  "empty_dict": {}
			}
			""";
		var node = JsonNode.Parse(json)!;

		var constValue = node.ToConstNodeValue()!;
		Assert.True(JsonNode.DeepEquals(node, constValue.ToJsonNode()));

		var reactiveValue = node.ToReactiveNodeValue()!;
		Assert.True(JsonNode.DeepEquals(node, reactiveValue.ToJsonNode()));
	}

	[Fact]
	public void Bson_ComplexValue_RoundTripsThroughBsonValue()
	{
		var value = BuildComplex();

		var back = value.ToBsonValue().ToConstNodeValue();

		AssertSameValue(value, back);
	}

	[Fact]
	public void Bson_HandWrittenDocument_ConvertsToConst()
	{
		var doc = new BsonDocument
		{
			["i"] = 42,
			["d"] = 3.5,
			["s"] = "text",
			["b"] = true,
			["n"] = BsonValue.Null,
			["arr"] = new BsonArray { 1, "two" },
		};

		var value = doc.ToConstNodeValue();
		var dict = Assert.IsType<ConstNodeDictionaryValue>(value);

		Assert.Equal(42.0, Assert.IsType<ConstNodeNumberValue>(dict.Items["i"]).Value);
		Assert.Equal(3.5, Assert.IsType<ConstNodeNumberValue>(dict.Items["d"]).Value);
		Assert.Equal("text", Assert.IsType<ConstNodeStringValue>(dict.Items["s"]).Value);
		Assert.True(Assert.IsType<ConstNodeBooleanValue>(dict.Items["b"]).Value);
		Assert.IsType<ConstNodeNullValue>(dict.Items["n"]);
		Assert.Equal(2, Assert.IsType<ConstNodeArrayValue>(dict.Items["arr"]).Items.Count);
	}

	[Fact]
	public void Yaml_ComplexValue_RoundTripsThroughYamlText()
	{
		var value = BuildComplex();

		var yamlText = EmitYaml(value.ToYamlNode());
		var back = ParseYaml(yamlText).ToConstNodeValue();

		AssertSameValue(value, back);
	}

	[Fact]
	public void Yaml_StringLikeScalars_AreQuotedToPreserveSemantics()
	{
		var value = Dict(
			("number-text", S("123")),
			("bool-text", S("true")),
			("null-text", S("null")),
			("empty-text", S("")),
			("number", N(123)),
			("boolean", B(false)));

		var yamlText = EmitYaml(value.ToYamlNode());
		var back = ParseYaml(yamlText).ToConstNodeValue();

		AssertSameValue(value, back);
		Assert.Contains("'123'", yamlText);
	}

	[Fact]
	public void Lua_ComplexValue_RoundTripsThroughLuaValue()
	{
		// Lua has no null and cannot distinguish an empty table from an empty dictionary/array,
		// so nulls and empty containers are excluded from this round-trip.
		var value = BuildComplex(includeEmptyContainers: false, includeNulls: false);

		var back = value.ToLuaValue().ToConstNodeValue();

		AssertSameValue(value, back);
	}

	[Fact]
	public void ConstReactive_ComplexValue_RoundTripsWithDeepCopyAndMutation()
	{
		var constValue = BuildComplex();
		Assert.Same(constValue, constValue.ToConstNodeValue());

		var reactive = Assert.IsType<ReactiveNodeDictionaryValue>(constValue.ToReactiveNodeValue());
		Assert.Same(reactive, reactive.ToReactiveNodeValue());

		// Mutating the reactive copy must not affect the original const value.
		var meta = Assert.IsType<ReactiveNodeDictionaryValue>(reactive.Items["meta"]);
		var nested = Assert.IsType<ReactiveNodeDictionaryValue>(meta.Items["nested"]);
		Assert.IsType<ReactiveNodeNumberValue>(nested.Items["depth"]).Value = 5.25;

		var constBack = Assert.IsType<ConstNodeDictionaryValue>(reactive.ToConstNodeValue());
		var nestedConst = (ConstNodeDictionaryValue)((ConstNodeDictionaryValue)constBack.Items["meta"]).Items["nested"];
		Assert.Equal(5.25, Assert.IsType<ConstNodeNumberValue>(nestedConst.Items["depth"]).Value);

		var originalNested = (ConstNodeDictionaryValue)((ConstNodeDictionaryValue)((ConstNodeDictionaryValue)constValue).Items["meta"]).Items["nested"];
		Assert.Equal(0.001, Assert.IsType<ConstNodeNumberValue>(originalNested.Items["depth"]).Value);
	}

	[Fact]
	public void Reactive_Clone_CreatesDeepCopyIndependentFromOriginal()
	{
		var original = (ReactiveNodeDictionaryValue)BuildComplex().ToReactiveNodeValue(clone: true)!;

		var clone = (ReactiveNodeDictionaryValue)original.ToReactiveNodeValue(clone: true)!;
		Assert.NotSame(original, clone);
		AssertSameValue(original, clone);

		// Mutating the clone must not affect the original.
		var cloneNested = (ReactiveNodeDictionaryValue)((ReactiveNodeDictionaryValue)clone.Items["meta"]).Items["nested"];
		Assert.IsType<ReactiveNodeNumberValue>(cloneNested.Items["depth"]).Value = 9.5;

		var originalNested = (ReactiveNodeDictionaryValue)((ReactiveNodeDictionaryValue)original.Items["meta"]).Items["nested"];
		Assert.Equal(0.001, Assert.IsType<ReactiveNodeNumberValue>(originalNested.Items["depth"]).Value);
	}

	private static string EmitYaml(YamlNode node)
	{
		var stream = new YamlStream(new YamlDocument(node));
		using var writer = new StringWriter();
		stream.Save(writer, assignAnchors: false);
		return writer.ToString();
	}

	private static YamlNode ParseYaml(string text)
	{
		var stream = new YamlStream();
		stream.Load(new StringReader(text));
		return stream.Documents[0].RootNode;
	}
}
