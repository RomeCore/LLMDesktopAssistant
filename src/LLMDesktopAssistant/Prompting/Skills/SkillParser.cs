using LLMDesktopAssistant.Services;
using RCParsing;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LLMDesktopAssistant.Prompting.Skills
{
	[Service(typeof(ISkillParser))]
	public class SkillParser : ISkillParser
	{
		private readonly Parser _parser;

		private static readonly IDeserializer _frontmatterDeserializer = new DeserializerBuilder()
			.IgnoreUnmatchedProperties()
			.WithNamingConvention(HyphenatedNamingConvention.Instance)
			.Build();

		private class FrontmatterDto
		{
			public string? Name { get; set; }
			public string? Description { get; set; }
			public string? Compatibility { get; set; }
			public string? License { get; set; }
			public List<string>? Tags { get; set; }
			public Dictionary<string, string>? Metadata { get; set; }
		}

		public SkillParser()
		{
			var builder = new ParserBuilder();

			builder.Settings.Skip(b => b.Whitespaces(), ParserSkippingStrategy.TryParseThenSkip);

			builder.CreateMainRule()
				.Optional(b => b
					.Literal("---")
					.TextUntil("---").Label("yaml")
					.Literal("---")
					.Transform(v => v["yaml"].Text)
				).Label("yaml")

				.Optional(b => b
					.Literal("#").TextUntil("\n", "\r", "\r\n").Optional(b => b.Whitespaces())
					.Transform(v => v[1].Text)
				).Label("name")

				.Optional(b => b
					.OneOrMoreSeparated(b => b.TextUntil("\n", "\r", "\r\n"), s => s.Newline()).Optional(b => b.Whitespaces())
					.Transform(v => v[0].Text)
				).Label("desc")

				.AllText()
				
				.Transform(v =>
				{
					var diagnosticCodes = SkillDiagnosticCode.None;
					Exception? exception = null;

					var fullpath = v.GetParsingParameter<string>();
					var yaml = v.TryGetValue<string>("yaml");
					var fallbackName = v.TryGetValue<string>("name");
					var fallbackDesc = v.TryGetValue<string>("desc");
					var homeDir = Path.GetDirectoryName(fullpath);
					var dirName = Path.GetFileName(homeDir);

					FrontmatterDto? frontmatter = null;
					YamlMappingNode? frontmatterMap = null;

					if (!string.IsNullOrWhiteSpace(yaml))
					{
						try
						{
							frontmatter = _frontmatterDeserializer.Deserialize<FrontmatterDto>(yaml);
							frontmatterMap = _frontmatterDeserializer.Deserialize<YamlMappingNode>(yaml);
						}
						catch (Exception ex)
						{
							diagnosticCodes |= SkillDiagnosticCode.YamlParsingError;
							exception = ex;
						}
					}
					else
					{
						diagnosticCodes |= SkillDiagnosticCode.MissingYaml;
					}

					string? name = frontmatter?.Name ?? fallbackName ?? dirName;
					string? description = frontmatter?.Description ?? fallbackDesc;
					string body = v.Text[v["yaml"].EndIndex..];

					if (string.IsNullOrEmpty(name))
					{
						diagnosticCodes |= SkillDiagnosticCode.MissingName;
						name = "unknown";
					}
					if (string.IsNullOrEmpty(frontmatter?.Name))
					{
						diagnosticCodes |= SkillDiagnosticCode.MissingYamlName;
					}
					if (!SkillName.IsValidSkillName(name))
					{
						name = SkillName.ToValidSkillName(name);
						diagnosticCodes |= SkillDiagnosticCode.NameFormatError;
					}
					if (name != dirName)
					{
						diagnosticCodes |= SkillDiagnosticCode.NameDirectoryMismatch;
					}
					if (string.IsNullOrEmpty(description))
					{
						diagnosticCodes |= SkillDiagnosticCode.MissingDescription;
						description = string.Empty;
					}
					if (string.IsNullOrEmpty(frontmatter?.Description))
					{
						diagnosticCodes |= SkillDiagnosticCode.MissingYamlDescription;
					}

					if (frontmatter != null)
					{
						try
						{
							var metadataBuilder = ImmutableDictionary.CreateBuilder<SkillMetadataType, string>();
							var additionalMetadataBuilder = ImmutableDictionary.CreateBuilder<string, string>();
							var allowedToolsBuilder = ImmutableList.CreateBuilder<string>();
							var tagsBuilder = ImmutableList.CreateBuilder<string>();
							var additionalPropertiesBuilder = ImmutableDictionary.CreateBuilder<string, YamlNode>();

							if (!string.IsNullOrEmpty(frontmatter.Compatibility))
								metadataBuilder.Add(SkillMetadataType.Compatibility, frontmatter.Compatibility);
							if (!string.IsNullOrEmpty(frontmatter.License))
								metadataBuilder.Add(SkillMetadataType.License, frontmatter.License);

							foreach (var (key, value) in frontmatter.Metadata ?? [])
							{
								switch (key)
								{
									case "author":
										metadataBuilder.Add(SkillMetadataType.Author, value);
										break;
									case "version":
										metadataBuilder.Add(SkillMetadataType.Version, value);
										break;
									default:
										additionalMetadataBuilder.Add(key, value);
										break;
								}
							}

							if (frontmatterMap!.Children.TryGetValue("allowed-tools", out var allowedTools))
							{
								if (allowedTools is YamlScalarNode allowedToolsStrNode)
								{
									foreach (var tool in _parser!.FindAllMatches<string>("allowed_tool", allowedToolsStrNode.Value ?? ""))
									{
										if (!string.IsNullOrWhiteSpace(tool))
											allowedToolsBuilder.Add(tool.Trim());
									}
								}
								else if (allowedTools is YamlSequenceNode allowedToolsSeqNode)
								{
									foreach (var node in allowedToolsSeqNode)
									{
										if (node is YamlScalarNode s && !string.IsNullOrWhiteSpace(s.Value))
											allowedToolsBuilder.Add(s.Value.Trim());
									}
								}
							}

							if (frontmatter.Tags is not null)
							{
								foreach (var tag in frontmatter.Tags)
								{
									tagsBuilder.Add(tag.Trim());
								}
							}

							if (frontmatterMap?.Children is not null)
							{
								foreach (var (key, value) in frontmatterMap.Children)
								{
									var strKey = (string)key!;
									switch (strKey)
									{
										case "name":
										case "description":
										case "compatibility":
										case "license":
										case "allowed-tools":
										case "tags":
										case "metadata":
											continue;

										default:
											additionalPropertiesBuilder.Add(strKey, value);
											break;
									}
								}
							}

							return new SkillInfo
							{
								Name = name.Trim(),
								Description = description.Trim(),
								BodyGetter = new(() => body),
								Path = fullpath,
								HomeDirectory = homeDir,
								Metadata = metadataBuilder.ToImmutableDictionary(),
								AdditionalMetadata = additionalMetadataBuilder.ToImmutableDictionary(),
								AllowedTools = allowedToolsBuilder.ToImmutableList(),
								Tags = tagsBuilder.ToImmutableList(),
								AdditionalProperties = additionalPropertiesBuilder.ToImmutableDictionary(),
								Diagnostic = diagnosticCodes != SkillDiagnosticCode.None ? new SkillDiagnostic
								{
									IsFatal = false,
									Codes = diagnosticCodes,
									Exception = exception
								} : null
							};
						}
						catch (Exception ex)
						{
							return new SkillInfo
							{
								Name = name.Trim(),
								Description = description.Trim(),
								BodyGetter = new(() => body),
								Path = fullpath,
								HomeDirectory = homeDir,
								Diagnostic = new SkillDiagnostic
								{
									IsFatal = false,
									Codes = diagnosticCodes | SkillDiagnosticCode.YamlDecodingError,
									Exception = ex
								}
							};
						}
					}

					return new SkillInfo
					{
						Name = name.Trim(),
						Description = description.Trim(),
						BodyGetter = new(() => body),
						Path = fullpath,
						HomeDirectory = homeDir,
						Diagnostic = new SkillDiagnostic
						{
							IsFatal = false,
							Codes = diagnosticCodes,
							Exception = exception
						}
					};
				});

			builder.CreateRule("allowed_tool")
				.UnicodeIdentifier()
				.Optional(b => b
					.Literal('(')
					.TextUntil([")"], allowsEmpty: true, consumeStopSequence: true)
				)
				.Transform(v => v.Text);

			_parser = builder.Build();
		}

		public SkillInfo Parse(string fullpath, string contents)
		{
			return _parser.Parse<SkillInfo>(contents, parameter: fullpath);
		}
	}
}
