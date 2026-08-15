using System.Text.Json.Nodes;
using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.Scripting.CSX;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Tools.Meta;

namespace LLMDesktopAssistant.Tests;

public class CSharpScriptMetaToolEngineTests
{
	private static readonly MetaToolSerializer Serializer = new();
	private static readonly IMetaToolEngineDescriptor Descriptor = new CSharpScriptMetaToolEngineDescriptor();
	private static readonly CSharpScriptMetaToolEngine Engine = new(new CSharpScriptService());

	private static MetaTool CreateTool(string executionCode) => new()
	{
		Name = "get_weather",
		IsLocal = false,
		Title = "Weather Checker",
		Description = "Gets the current weather for a location.",
		Category = "weather",
		ApprovalLevel = ToolApprovalLevel.PolicyBased,
		Behaviours = ToolBehaviour.None,
		ArgumentSchema = new JsonObject
		{
			["type"] = "object",
			["properties"] = new JsonObject
			{
				["location"] = new JsonObject { ["type"] = "string" }
			}
		},
		ScriptLanguage = ScriptLanguageType.CSharpScript,
		ExecutionCode = executionCode
	};

	private static ToolExecutionContext CreateDummyContext()
	{
		var toolInfo = new ToolInfo
		{
			Name = "get_weather",
			DescriptionGetter = () => "Gets the current weather for a location.",
			ArgumentSchema = new JsonObject { ["type"] = "object" },
			Executor = (_, _, _) => Task.FromResult(new ReactiveToolResult())
		};
		return ToolExecutionContext.CreateDummy(toolInfo, null, null);
	}

	[Fact]
	public void Deserialize_CSXFrontmatter_ParsesAllFields()
	{
		var content = """
			/*
			title: Weather Checker
			description: Gets the current weather for a location.
			category: weather
			approval_level: always-ask
			*/
			var city = (string?)ToolArgs?["city"];
			Result.Write("City: " + city);
			""";

		var tool = Serializer.Deserialize(content, "get_weather", true, Descriptor);

		Assert.Equal(ScriptLanguageType.CSharpScript, tool.ScriptLanguage);
		Assert.Equal("Weather Checker", tool.Title);
		Assert.Equal(ToolApprovalLevel.AlwaysAsk, tool.ApprovalLevel);
		Assert.Equal("""
			var city = (string?)ToolArgs?["city"];
			Result.Write("City: " + city);
			""", tool.ExecutionCode);
	}

	[Fact]
	public void Serialize_RoundTrip_PreservesAllFields()
	{
		var tool = CreateTool("var city = (string?)ToolArgs?[\"city\"];\nResult.Write(\"City: \" + city);");

		var deserialized = Serializer.Deserialize(Serializer.Serialize(tool, Descriptor), "get_weather", true, Descriptor);

		Assert.Equal(tool.ScriptLanguage, deserialized.ScriptLanguage);
		Assert.Equal(tool.Title, deserialized.Title);
		Assert.Equal(tool.ExecutionCode, deserialized.ExecutionCode);
		Assert.Equal(tool.ArgumentSchema?.ToJsonString(), deserialized.ArgumentSchema?.ToJsonString());
	}

	[Fact]
	public void Serialize_CSX_UsesBlockCommentFrontmatter()
	{
		var content = Serializer.Serialize(CreateTool("return 42;"), Descriptor);

		Assert.StartsWith("/*", content);
		Assert.Contains("title: Weather Checker", content);
		Assert.Contains("*/", content);
		Assert.EndsWith("return 42;", content.TrimEnd());
	}

	[Fact]
	public async Task Execute_Script_StreamsResultAndReturnsStructuredValue()
	{
		var tool = CreateTool("""
			var city = (string?)ToolArgs?["city"];
			Result.Write("City: " + city);
			Result.SetStatus("Map", "Looking up...");
			return new { ok = true, city };
			""");
		var executor = Engine.CreateExecutor(tool);
		var args = JsonNode.Parse("""{ "city": "New York" }""");

		var reactiveResult = await executor(args, CreateDummyContext(), CancellationToken.None);
		var success = await reactiveResult.Completion;

		Assert.True(success);
		Assert.Contains(reactiveResult.ResultContentLines, line => line == "City: New York");
		Assert.Equal("Map", reactiveResult.StatusIcon?.ToString());
		Assert.Equal("Looking up...", reactiveResult.StatusTitle);
		Assert.Equal(true, reactiveResult.StructuredResult?["ok"]?.GetValue<bool>());
		Assert.Equal("New York", reactiveResult.StructuredResult?["city"]?.GetValue<string>());
	}

	[Fact]
	public async Task Execute_Script_MissingOptionalArguments_DoesNotFail()
	{
		var tool = CreateTool("""
			var days = (int?)ToolArgs?["days"] ?? 10;
			Result.Write("Days: " + days);
			return days;
			""");
		var executor = Engine.CreateExecutor(tool);

		var reactiveResult = await executor(null, CreateDummyContext(), CancellationToken.None);
		var success = await reactiveResult.Completion;

		Assert.True(success);
		Assert.Contains(reactiveResult.ResultContentLines, line => line == "Days: 10");
		Assert.Equal(10, reactiveResult.StructuredResult?.GetValue<int>());
	}

	[Fact]
	public async Task Execute_ScriptWithCompilationError_Fails()
	{
		var tool = CreateTool("var x = ;");
		var executor = Engine.CreateExecutor(tool);

		var reactiveResult = await executor(null, CreateDummyContext(), CancellationToken.None);
		var success = await reactiveResult.Completion;

		Assert.False(success);
		Assert.Contains(reactiveResult.ResultContentLines, line => line.Contains("Compilation errors"));
	}

	[Fact]
	public async Task Execute_ScriptThrowingException_Fails()
	{
		var tool = CreateTool("throw new InvalidOperationException(\"boom\");");
		var executor = Engine.CreateExecutor(tool);

		var reactiveResult = await executor(null, CreateDummyContext(), CancellationToken.None);
		var success = await reactiveResult.Completion;

		Assert.False(success);
		Assert.Contains(reactiveResult.ResultContentLines, line => line.Contains("boom"));
	}
}
