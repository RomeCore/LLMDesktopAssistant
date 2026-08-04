using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using LLMDesktopAssistant.Services;
using RCParsing;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LLMDesktopAssistant.Tools.Meta
{
	[Service(typeof(IMetaToolSerializer))]
	public class MetaToolSerializer : IMetaToolSerializer
	{
		private static readonly JsonSerializerOptions _jsonOptions = new()
		{
			Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
			WriteIndented = true
		};

		private static readonly ConcurrentDictionary<IMetaToolEngineDescriptor, Parser> _frontmatterExtractors = [];

		private static readonly ISerializer _yamlSerializer = new SerializerBuilder()
			.ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
			.WithNamingConvention(UnderscoredNamingConvention.Instance)
			.Build();

		private static readonly IDeserializer _yamlDeserializer = new DeserializerBuilder()
			.IgnoreUnmatchedProperties()
			.WithNamingConvention(UnderscoredNamingConvention.Instance)
			.Build();

		private class FrontmatterDto
		{
			public string Title { get; set; } = string.Empty;
			public string Description { get; set; } = string.Empty;
			public string Category { get; set; } = string.Empty;
			public string ApprovalLevel { get; set; } = "policy-based";
			public string[]? Behaviours { get; set; }
			public string ArgumentSchema { get; set; } = string.Empty;
		}

		private static Parser GetFrontmatterExtractor(IMetaToolEngineDescriptor engineDescriptor)
		{
			return _frontmatterExtractors.GetOrAdd(engineDescriptor, descriptor =>
			{
				var builder = new ParserBuilder();

				builder.Settings.Skip(b => b.Whitespaces(), ParserSkippingStrategy.TryParseThenSkip);

				builder.CreateMainRule()
					.Literal(descriptor.FrontmatterStart)
					.TextUntil(descriptor.FrontmatterEnd).Label("yaml")
					.Literal(descriptor.FrontmatterEnd)
					.AllText().Label("code");

				return builder.Build();
			});
		}

		/// <inheritdoc/>
		public MetaTool Deserialize(string fileContents, string name, bool isLocal, IMetaToolEngineDescriptor engineDescriptor)
		{
			var frontmatterExtractor = GetFrontmatterExtractor(engineDescriptor);
			var extractionResult = frontmatterExtractor.Parse(fileContents);
			var frontmatterText = extractionResult["yaml"].Text;
			var executionCode = extractionResult["code"].Text;

			var frontmatter = _yamlDeserializer.Deserialize<FrontmatterDto>(frontmatterText);
			var argumentSchema = string.IsNullOrWhiteSpace(frontmatter.ArgumentSchema)
				? new JsonObject { ["type"] = "object", ["additionalProperties"] = false }
				: JsonSerializer.Deserialize<JsonObject>(frontmatter.ArgumentSchema, _jsonOptions)
					?? new JsonObject { ["type"] = "object", ["additionalProperties"] = false };

			return new MetaTool
			{
				Name = name,
				IsLocal = isLocal,
				Title = frontmatter.Title,
				Description = frontmatter.Description,
				Category = frontmatter.Category,
				ApprovalLevel = MetaToolHumanizedEnumNames.DeserializeApprovalLevel(frontmatter.ApprovalLevel),
				Behaviours = MetaToolHumanizedEnumNames.ResolveBehaviours(frontmatter.Behaviours),
				ArgumentSchema = argumentSchema,
				ScriptLanguage = engineDescriptor.Language,
				ExecutionCode = executionCode.Trim()
			};
		}

		/// <inheritdoc/>
		public string Serialize(MetaTool tool, IMetaToolEngineDescriptor engineDescriptor)
		{
			var argumentSchemaText = JsonSerializer.Serialize(tool.ArgumentSchema, _jsonOptions);
			var frontmatter = new FrontmatterDto
			{
				Title = tool.Title,
				Description = tool.Description,
				Category = tool.Category,
				ApprovalLevel = MetaToolHumanizedEnumNames.SerializeApprovalLevel(tool.ApprovalLevel),
				Behaviours = MetaToolHumanizedEnumNames.SerializeBehaviours(tool.Behaviours),
				ArgumentSchema = argumentSchemaText
			};
			var frontmatterText = _yamlSerializer.Serialize(frontmatter).TrimEnd();

			return $"""
				{engineDescriptor.FrontmatterStart}
				{frontmatterText}
				{engineDescriptor.FrontmatterEnd}
				{tool.ExecutionCode}
				""";
		}
	}
}
