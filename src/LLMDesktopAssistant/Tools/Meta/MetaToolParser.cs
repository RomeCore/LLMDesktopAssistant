using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using LLMDesktopAssistant.Services;
using RCLargeLanguageModels.Tools;
using RCParsing;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LLMDesktopAssistant.Tools.Meta
{
	/// <summary>
	/// The default <see cref="IMetaToolParser"/> implementation that extracts the YAML frontmatter
	/// from a meta tool file and validates its fields.
	/// </summary>
	[Service(typeof(IMetaToolParser))]
	public class MetaToolParser : IMetaToolParser
	{
		private static readonly JsonSerializerOptions _jsonOptions = new()
		{
			Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
			WriteIndented = true
		};

		private static readonly ConcurrentDictionary<IMetaToolEngineDescriptor, Parser> _frontmatterExtractors = [];

		private static readonly IDeserializer _yamlDeserializer = new DeserializerBuilder()
			.IgnoreUnmatchedProperties()
			.WithNamingConvention(UnderscoredNamingConvention.Instance)
			.Build();

		private static readonly ISerializer _yamlSerializer = new SerializerBuilder()
			.ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
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
		public MetaToolInfo Parse(string filePath, string contents, MetaToolSource source, IMetaToolEngineDescriptor engineDescriptor)
		{
			var name = Path.GetFileNameWithoutExtension(filePath);
			var codes = MetaToolDiagnosticCode.None;

			if (!ToolName.CheckValid(name))
				codes |= MetaToolDiagnosticCode.NameFormatError;

			var extractor = GetFrontmatterExtractor(engineDescriptor);

			string frontmatterText;
			string executionCode;
			try
			{
				var extractionResult = extractor.Parse(contents);
				frontmatterText = extractionResult["yaml"].Text;
				executionCode = extractionResult["code"].Text;
			}
			catch (Exception ex)
			{
				return CreateDiagnosticInfo(name, filePath, source, engineDescriptor,
					new MetaToolDiagnostic
					{
						IsFatal = true,
						Codes = codes | MetaToolDiagnosticCode.MissingFrontmatter,
						Exception = ex
					});
			}

			if (string.IsNullOrWhiteSpace(frontmatterText))
			{
				return CreateDiagnosticInfo(name, filePath, source, engineDescriptor,
					new MetaToolDiagnostic
					{
						IsFatal = true,
						Codes = codes | MetaToolDiagnosticCode.MissingFrontmatter,
						Exception = null
					});
			}

			FrontmatterDto frontmatter;
			try
			{
				frontmatter = _yamlDeserializer.Deserialize<FrontmatterDto>(frontmatterText);
			}
			catch (Exception ex)
			{
				return CreateDiagnosticInfo(name, filePath, source, engineDescriptor,
					new MetaToolDiagnostic
					{
						IsFatal = true,
						Codes = codes | MetaToolDiagnosticCode.FrontmatterParsingError,
						Exception = ex
					});
			}

			ToolApprovalLevel approvalLevel;
			try
			{
				approvalLevel = MetaToolHumanizedEnumNames.DeserializeApprovalLevel(frontmatter.ApprovalLevel);
			}
			catch
			{
				approvalLevel = ToolApprovalLevel.PolicyBased;
				codes |= MetaToolDiagnosticCode.InvalidApprovalLevel;
			}

			ToolBehaviour behaviours;
			try
			{
				behaviours = MetaToolHumanizedEnumNames.ResolveBehaviours(frontmatter.Behaviours);
			}
			catch
			{
				behaviours = ToolBehaviour.None;
				codes |= MetaToolDiagnosticCode.InvalidBehaviours;
			}

			JsonObject? argumentSchema = null;
			if (!string.IsNullOrWhiteSpace(frontmatter.ArgumentSchema))
			{
				try
				{
					argumentSchema = JsonSerializer.Deserialize<JsonObject>(frontmatter.ArgumentSchema, _jsonOptions);
				}
				catch (Exception ex)
				{
					return CreateDiagnosticInfo(name, filePath, source, engineDescriptor,
						new MetaToolDiagnostic
						{
							IsFatal = true,
							Codes = codes | MetaToolDiagnosticCode.InvalidArgumentSchema,
							Exception = ex
						});
				}
			}

			argumentSchema ??= new JsonObject
			{
				["type"] = "object",
				["additionalProperties"] = false
			};

			var diagnostic = codes == MetaToolDiagnosticCode.None
				? null
				: new MetaToolDiagnostic
				{
					IsFatal = false,
					Codes = codes,
					Exception = null
				};

			return new MetaToolInfo
			{
				Name = name,
				Title = frontmatter.Title,
				Description = frontmatter.Description,
				Category = frontmatter.Category,
				ApprovalLevel = approvalLevel,
				Behaviours = behaviours,
				ArgumentSchema = argumentSchema,
				ScriptLanguage = engineDescriptor.Language,
				ExecutionCode = executionCode.Trim(),
				Source = source,
				Path = filePath,
				Diagnostic = diagnostic
			};
		}

		private static MetaToolInfo CreateDiagnosticInfo(string name, string filePath, MetaToolSource source,
			IMetaToolEngineDescriptor engineDescriptor, MetaToolDiagnostic diagnostic) => new()
		{
			Name = name,
			Source = source,
			Path = filePath,
			ScriptLanguage = engineDescriptor.Language,
			Diagnostic = diagnostic
		};

		/// <inheritdoc/>
		public string Serialize(MetaToolInfo tool, IMetaToolEngineDescriptor engineDescriptor)
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
