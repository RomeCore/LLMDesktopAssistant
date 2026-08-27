using System.Text.Json.Nodes;
using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Tools.Meta;

namespace LLMDesktopAssistant.Tests.MetaTools;

public class MetaToolParserTests
{
	private sealed class TestEngineDescriptor : IMetaToolEngineDescriptor
	{
		public required ScriptLanguageType Language { get; init; }
		public string MainExtension => Extensions[0];
		public required string[] Extensions { get; init; }
		public required string FrontmatterStart { get; init; }
		public required string FrontmatterEnd { get; init; }
		public string Examples => string.Empty;
		public string Template => string.Empty;
	}

	private static readonly MetaToolParser Parser = new();

	private static readonly IMetaToolEngineDescriptor LuaDescriptor = new TestEngineDescriptor
	{
		Language = ScriptLanguageType.Lua,
		Extensions = [".lua"],
		FrontmatterStart = "--[[",
		FrontmatterEnd = "]]"
	};

	private static readonly IMetaToolEngineDescriptor PythonDescriptor = new TestEngineDescriptor
	{
		Language = ScriptLanguageType.Python,
		Extensions = [".py"],
		FrontmatterStart = "\"\"\"",
		FrontmatterEnd = "\"\"\""
	};

	private static MetaToolInfo CreateTool(
		ToolApprovalLevel approvalLevel = ToolApprovalLevel.PolicyBased,
		ToolBehaviour behaviours = ToolBehaviour.None,
		ScriptLanguageType language = ScriptLanguageType.Lua)
	{
		return new MetaToolInfo
		{
			Name = "get_weather",
			Title = "Weather Checker",
			Description = "Gets the current weather for a location.",
			Category = "weather",
			ApprovalLevel = approvalLevel,
			Behaviours = behaviours,
			ArgumentSchema = JsonNode.Parse("""
				{
					"type": "object",
					"properties": {
						"location": { "type": "string", "description": "The location to check." }
					},
					"required": ["location"]
				}
				""") as JsonObject,
			ScriptLanguage = language,
			ExecutionCode = """
				local url = "https://api.weather.com/" .. tool_args.location
				local result = await web.fetch(url)
				print(result)
				"""
		};
	}

	private static string BuildLuaFile(string yaml, string code) => $"--[[\n{yaml}\n]]\n{code}";

	private static string BuildPythonFile(string yaml, string code) => $"\"\"\"\n{yaml}\n\"\"\"\n{code}";

	private static MetaToolInfo Deserialize(string content, string name = "get_weather", MetaToolSource source = MetaToolSource.UserProfile,
		IMetaToolEngineDescriptor? descriptor = null)
	{
		return Parser.Parse(name, content, source, descriptor ?? LuaDescriptor);
	}

	[Fact]
	public void Deserialize_FullFrontmatter_ParsesAllFields()
	{
		var content = BuildLuaFile("""
			title: Weather Checker
			description: Gets the current weather for a location.
			category: weather
			approval_level: always-ask
			behaviours:
			  - internet-access
			  - long-running-task
			argument_schema: '{"type":"object","properties":{"location":{"type":"string"}},"required":["location"]}'
			""", "print('hi')");

		var tool = Deserialize(content, source: MetaToolSource.UserProfile);

		Assert.Equal("get_weather", tool.Name);
		Assert.Equal(MetaToolSource.UserProfile, tool.Source);
		Assert.Equal("Weather Checker", tool.Title);
		Assert.Equal("Gets the current weather for a location.", tool.Description);
		Assert.Equal("weather", tool.Category);
		Assert.Equal(ToolApprovalLevel.AlwaysAsk, tool.ApprovalLevel);
		Assert.Equal(ToolBehaviour.InternetAccess | ToolBehaviour.LongRunningTask, tool.Behaviours);
		Assert.Equal(ScriptLanguageType.Lua, tool.ScriptLanguage);
		Assert.Equal("print('hi')", tool.ExecutionCode);
		Assert.Equal("location", tool.ArgumentSchema?["required"]?[0]?.GetValue<string>());
	}

	[Fact]
	public void Serialize_RoundTrip_PreservesAllFields()
	{
		var tool = CreateTool(ToolApprovalLevel.PolicyAutoDisallowUnlessApproved,
			ToolBehaviour.InternetAccess | ToolBehaviour.ExecuteExternalProcess);

		var deserialized = Deserialize(Parser.Serialize(tool, LuaDescriptor));

		Assert.Equal(tool.Name, deserialized.Name);
		Assert.Equal(tool.Title, deserialized.Title);
		Assert.Equal(tool.Description, deserialized.Description);
		Assert.Equal(tool.Category, deserialized.Category);
		Assert.Equal(tool.ApprovalLevel, deserialized.ApprovalLevel);
		Assert.Equal(tool.Behaviours, deserialized.Behaviours);
		Assert.Equal(tool.ScriptLanguage, deserialized.ScriptLanguage);
		Assert.Equal(tool.ExecutionCode, deserialized.ExecutionCode);
		Assert.Equal(tool.ArgumentSchema?.ToJsonString(), deserialized.ArgumentSchema?.ToJsonString());
	}

	[Fact]
	public void Serialize_ContainsFrontmatterAndCode()
	{
		var content = Parser.Serialize(CreateTool(), LuaDescriptor);

		Assert.StartsWith("--[[", content);
		Assert.Contains("title: Weather Checker", content);
		Assert.Contains("approval_level: policy-based", content);
		Assert.Contains("argument_schema:", content);
		Assert.Contains("]]", content);
		Assert.Contains("local url =", content);
	}

	[Theory]
	[InlineData(ToolApprovalLevel.PolicyBased)]
	[InlineData(ToolApprovalLevel.PolicyApproveOrAsk)]
	[InlineData(ToolApprovalLevel.PolicyAutoApproveUnlessDisallowed)]
	[InlineData(ToolApprovalLevel.PolicyAutoDisallowUnlessApproved)]
	[InlineData(ToolApprovalLevel.PolicyAskOrDisallow)]
	[InlineData(ToolApprovalLevel.AlwaysApprove)]
	[InlineData(ToolApprovalLevel.AlwaysAsk)]
	[InlineData(ToolApprovalLevel.AlwaysDisallow)]
	public void ApprovalLevel_RoundTrip_PreservesLevel(ToolApprovalLevel level)
	{
		var tool = CreateTool(approvalLevel: level);

		var serialized = Parser.Serialize(tool, LuaDescriptor);
		var deserialized = Deserialize(serialized);

		Assert.Equal(level, deserialized.ApprovalLevel);
	}

	[Fact]
	public void AllBehaviourFlags_RoundTrip_PreserveFlags()
	{
		foreach (var flag in Enum.GetValues<ToolBehaviour>().Where(IsSingleFlag))
		{
			var tool = CreateTool(behaviours: flag);

			var deserialized = Deserialize(Parser.Serialize(tool, LuaDescriptor));

			Assert.Equal(flag, deserialized.Behaviours);
		}
	}

	[Fact]
	public void Deserialize_InvalidApprovalLevel_FallsBackToPolicyBased()
	{
		var content = BuildLuaFile("""
			title: Tool
			description: desc
			category: general
			approval_level: nonsense
			""", "print('hi')");

		Assert.Equal(ToolApprovalLevel.PolicyBased, Deserialize(content).ApprovalLevel);
	}

	[Fact]
	public void Deserialize_UnknownBehaviour_IsIgnored()
	{
		var content = BuildLuaFile("""
			title: Tool
			description: desc
			category: general
			behaviours:
			  - internet-access
			  - bogus
			""", "print('hi')");

		Assert.Equal(ToolBehaviour.InternetAccess, Deserialize(content).Behaviours);
	}

	[Fact]
	public void Deserialize_BehaviourPascalCase_FallsBackToEnumParse()
	{
		var content = BuildLuaFile("""
			title: Tool
			description: desc
			category: general
			behaviours:
			  - InternetAccess
			""", "print('hi')");

		Assert.Equal(ToolBehaviour.InternetAccess, Deserialize(content).Behaviours);
	}

	[Fact]
	public void Deserialize_MissingArgumentSchema_UsesDefaultSchema()
	{
		var content = BuildLuaFile("""
			title: Tool
			description: desc
			category: general
			""", "print('hi')");

		var tool = Deserialize(content);

		Assert.NotNull(tool.ArgumentSchema);
		Assert.Equal("object", tool.ArgumentSchema?["type"]?.GetValue<string>());
	}

	[Fact]
	public void Deserialize_UnknownYamlFields_AreIgnored()
	{
		var content = BuildLuaFile("""
			title: Tool
			description: desc
			category: general
			foo: bar
			""", "print('hi')");

		var tool = Deserialize(content);

		Assert.Null(tool.ApprovalLevel);
		Assert.Equal("Tool", tool.Title);
	}

	[Fact]
	public void Deserialize_PythonFrontmatter_ParsesAllFields()
	{
		var content = BuildPythonFile("""
			title: Py Tool
			description: Python tool.
			category: general
			approval_level: always-ask
			""", "print(tool_args)");

		var tool = Deserialize(content, descriptor: PythonDescriptor);

		Assert.Equal(ScriptLanguageType.Python, tool.ScriptLanguage);
		Assert.Equal("Py Tool", tool.Title);
		Assert.Equal(ToolApprovalLevel.AlwaysAsk, tool.ApprovalLevel);
		Assert.Equal("print(tool_args)", tool.ExecutionCode);
	}

	[Fact]
	public void Serialize_Python_UsesPythonFrontmatter()
	{
		var tool = CreateTool(language: ScriptLanguageType.Python);

		var content = Parser.Serialize(tool, PythonDescriptor);

		Assert.StartsWith("\"\"\"", content);
		Assert.Contains("title: Weather Checker", content);
	}

	[Fact]
	public void Serialize_NoBehaviours_OmitsBehaviorsKey()
	{
		var content = Parser.Serialize(CreateTool(), LuaDescriptor);

		Assert.DoesNotContain("behaviors", content);
	}

	[Fact]
	public void Serialize_WithBehaviours_WritesKebabCaseNames()
	{
		var tool = CreateTool(behaviours: ToolBehaviour.InternetAccess | ToolBehaviour.FileRead);

		var content = Parser.Serialize(tool, LuaDescriptor);

		Assert.Contains("internet-access", content);
		Assert.Contains("file-read", content);
	}

	private static bool IsSingleFlag(ToolBehaviour behaviour)
	{
		var value = (long)behaviour;
		return value != 0 && (value & (value - 1)) == 0;
	}
}
