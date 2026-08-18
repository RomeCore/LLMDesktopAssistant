using LLMDesktopAssistant.Agents.SubAgents;

namespace LLMDesktopAssistant.Tests.SubAgents;

public class SubAgentParserTests
{
	private static readonly SubAgentParser Parser = new();

	private static SubAgentInfo Parse(string content, string? path = null)
		=> Parser.Parse(path ?? "C:\\agents\\test-agent.md", content);

	[Fact]
	public void MinimalFrontmatter_ParsesNameAndDescription()
	{
		var subAgent = Parse("""
			---
			name: test-agent
			description: A test sub-agent for unit testing.
			---
			""");

		Assert.Equal("test-agent", subAgent.Name);
		Assert.Equal("A test sub-agent for unit testing.", subAgent.Description);
		Assert.Empty(subAgent.SystemPromptGetter());
	}

	[Fact]
	public void FullFrontmatter_ParsesAllFields()
	{
		var subAgent = Parse("""
			---
			name: pdf-processor
			description: Extract text from PDFs, fill forms, merge documents.
			model: gpt-4o
			license: MIT
			compatibility: Requires Python 3.14+
			allowed-tools: Bash(python:*) Read Write
			tags:
			  - pdf
			  - document
			  - extraction
			metadata:
			  author: example-org
			  version: "2.1"
			---
			""");

		Assert.Equal("pdf-processor", subAgent.Name);
		Assert.Equal("Extract text from PDFs, fill forms, merge documents.", subAgent.Description);
		Assert.Equal("gpt-4o", subAgent.Model);
		Assert.Equal("MIT", subAgent.Metadata[SubAgentMetadataType.License]);
		Assert.Equal("Requires Python 3.14+", subAgent.Metadata[SubAgentMetadataType.Compatibility]);
		Assert.Equal("example-org", subAgent.Metadata[SubAgentMetadataType.Author]);
		Assert.Equal("2.1", subAgent.Metadata[SubAgentMetadataType.Version]);
		Assert.Equal([new("Bash", "python:*"), new("Read"), new("Write")], subAgent.AllowedTools);
		Assert.Equal(["pdf", "document", "extraction"], subAgent.Tags);
	}

	[Fact]
	public void ToolsAsList_ParsesCorrectly()
	{
		var subAgent = Parse("""
			---
			name: multi-tool
			description: Sub-agent with multiple tools.
			allowed-tools:
			  - Bash(git:*)
			  - Read
			  - Write
			  - Bash(docker:*)
			---
			""");

		Assert.Equal([new("Bash", "git:*"), new("Read"), new("Write"), new("Bash", "docker:*")], subAgent.AllowedTools);
	}

	[Fact]
	public void ToolsAsString_ParsesCorrectly()
	{
		var subAgent = Parse("""
			---
			name: string-tools
			description: Sub-agent with space-separated tools.
			allowed-tools: Bash(git:*) Read Write
			available-tools: Bash(node:*) Python
			disallowed-tools: Bash(docker:*) Python
			---
			""");

		Assert.Equal([new("Bash", "git:*"), new("Read"), new("Write")], subAgent.AllowedTools);
		Assert.Equal([new("Bash", "node:*"), new("Python")], subAgent.AvailableTools);
		Assert.Equal([new("Bash", "docker:*"), new("Python")], subAgent.DisallowedTools);
	}

	[Fact]
	public void BodyAfterFrontmatter_IsPreserved()
	{
		var subAgent = Parse("""
			---
			name: body-test
			description: Sub-agent with body content.
			---
			# Instructions

			Step 1: Do something.
			Step 2: Do another thing.

			## Notes
			Some notes here.
			""");

		Assert.Contains("Step 1: Do something.", subAgent.SystemPromptGetter());
		Assert.Contains("Step 2: Do another thing.", subAgent.SystemPromptGetter());
		Assert.Contains("## Notes", subAgent.SystemPromptGetter());
	}

	[Fact]
	public void BodyWithoutFrontmatter_UsesHeaderAsFallbackName()
	{
		var subAgent = Parse("""
			# My Custom SubAgent

			This sub-agent does something useful.
			Use this sub-agent when you need to do X.

			Detailed instructions go here.
			""");

		Assert.Equal("my-custom-subagent", subAgent.Name); // Parser slugifies sub-agent name
		Assert.Equal("""
			This sub-agent does something useful.
			Use this sub-agent when you need to do X.
			""", subAgent.Description);
		Assert.Contains("Detailed instructions go here.", subAgent.SystemPromptGetter());
	}

	[Fact]
	public void BodyWithoutFrontmatterAndWithoutHeader_UsesFileName()
	{
		var subAgent = Parse("Just some plain text content.", "C:\\agents\\fallback-name.md");

		Assert.Equal("fallback-name", subAgent.Name);
		Assert.Equal("Just some plain text content.", subAgent.Description);
		Assert.Equal("Just some plain text content.", subAgent.SystemPromptGetter());
	}

	[Fact]
	public void NameFileMismatch_ReportsDiagnostic()
	{
		var subAgent = Parse("""
			---
			name: different-name
			description: Name does not match the file name.
			---
			""", "C:\\agents\\file-name.md");

		Assert.Equal("different-name", subAgent.Name);
		Assert.NotNull(subAgent.Diagnostic);
		Assert.True(subAgent.Diagnostic.Codes.HasFlag(SubAgentDiagnosticCode.NameFileMismatch));
	}

	[Fact]
	public void NameMatchesFileName_NoDiagnostic()
	{
		var subAgent = Parse("""
			---
			name: matching-name
			description: Name matches the file name.
			---
			""", "C:\\agents\\matching-name.md");

		Assert.Equal("matching-name", subAgent.Name);
		Assert.Null(subAgent.Diagnostic);
	}

	[Fact]
	public void Source_IsPassedThrough()
	{
		var subAgent = Parse("""
			---
			name: sourced-agent
			description: Sub-agent with a source.
			---
			""");

		Assert.Equal(SubAgentSource.Unknown, subAgent.Source);

		var sourcedSubAgent = Parser.Parse(
			"C:\\agents\\sourced-agent.md",
			"""
			---
			name: sourced-agent
			description: Sub-agent with a source.
			---
			""",
			SubAgentSource.WorkingDirectory);

		Assert.Equal(SubAgentSource.WorkingDirectory, sourcedSubAgent.Source);
	}

	[Fact]
	public void AdditionalMetadata_PreservesUnknownKeys()
	{
		var subAgent = Parse("""
			---
			name: custom-meta
			description: Sub-agent with custom metadata.
			metadata:
			  author: me
			  version: "1.0"
			  x-custom-field: custom-value
			  department: engineering
			---
			""");

		Assert.Equal("me", subAgent.Metadata[SubAgentMetadataType.Author]);
		Assert.Equal("1.0", subAgent.Metadata[SubAgentMetadataType.Version]);
		Assert.Equal("custom-value", subAgent.AdditionalMetadata["x-custom-field"]);
		Assert.Equal("engineering", subAgent.AdditionalMetadata["department"]);
	}

	[Fact]
	public void AdditionalProperties_StoresNonStandardRootFields()
	{
		var subAgent = Parse("""
			---
			name: extra-fields
			description: Sub-agent with extra root fields.
			priority: high
			category: testing
			x-my-field: hello
			---
			""");

		Assert.Equal("high", (string)subAgent.AdditionalProperties["priority"]!);
		Assert.Equal("testing", (string)subAgent.AdditionalProperties["category"]!);
		Assert.Equal("hello", (string)subAgent.AdditionalProperties["x-my-field"]!);
	}

	[Fact]
	public void Path_IsSet()
	{
		var path = "C:\\my-project\\.claude\\agents\\my-agent.md";
		var subAgent = Parse("""
			---
			name: my-agent
			description: Sub-agent with path.
			---
			""", path);

		Assert.Equal(path, subAgent.Path);
	}
}
