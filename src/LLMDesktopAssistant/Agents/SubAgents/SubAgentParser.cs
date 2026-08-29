using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Tools;
using RCParsing;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LLMDesktopAssistant.Agents.SubAgents
{
	[Service(typeof(ISubAgentParser))]
	public class SubAgentParser : ISubAgentParser
	{
		private readonly Parser _parser;

		private static readonly IDeserializer _frontmatterDeserializer = new DeserializerBuilder()
			.IgnoreUnmatchedProperties()
			.WithNamingConvention(HyphenatedNamingConvention.Instance)
			.Build();

		private class ParserParameter
		{
			public required string FullPath { get; init; }
			public required SubAgentSource Source { get; init; }
		}

		private class FrontmatterDto
		{
			public string? Name { get; set; }
			public string? Description { get; set; }
			public string? Compatibility { get; set; }
			public string? License { get; set; }
			public string? Model { get; set; }
			public List<string>? Tags { get; set; }
			public Dictionary<string, string>? Metadata { get; set; }
		}

		public SubAgentParser()
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
					var diagnosticCodes = SubAgentDiagnosticCode.None;
					Exception? exception = null;

					var parameter = v.GetParsingParameter<ParserParameter>();
					var fullpath = parameter.FullPath;
					var source = parameter.Source;
					var yaml = v.TryGetValue<string>("yaml");
					var fallbackName = v.TryGetValue<string>("name");
					var fallbackDesc = v.TryGetValue<string>("desc");
					var homeDir = Path.GetDirectoryName(fullpath);
					var fileName = Path.GetFileNameWithoutExtension(fullpath);

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
							diagnosticCodes |= SubAgentDiagnosticCode.YamlParsingError;
							exception = ex;
						}
					}
					else
					{
						diagnosticCodes |= SubAgentDiagnosticCode.MissingYaml;
					}

					string? name = frontmatter?.Name ?? fallbackName ?? fileName;
					string? description = frontmatter?.Description ?? fallbackDesc;
					string prompt = v.Span[v["yaml"].EndIndex..].Trim().ToString();

					if (string.IsNullOrEmpty(name))
					{
						diagnosticCodes |= SubAgentDiagnosticCode.MissingName;
						name = "unknown";
					}
					if (string.IsNullOrEmpty(frontmatter?.Name))
					{
						diagnosticCodes |= SubAgentDiagnosticCode.MissingYamlName;
					}
					if (!SubAgentName.IsValidSubAgentName(name))
					{
						name = SubAgentName.ToValidSubAgentName(name);
						diagnosticCodes |= SubAgentDiagnosticCode.NameFormatError;
					}
					if (name != fileName)
					{
						diagnosticCodes |= SubAgentDiagnosticCode.NameFileMismatch;
					}
					if (string.IsNullOrEmpty(description))
					{
						diagnosticCodes |= SubAgentDiagnosticCode.MissingDescription;
						description = string.Empty;
					}
					if (string.IsNullOrEmpty(frontmatter?.Description))
					{
						diagnosticCodes |= SubAgentDiagnosticCode.MissingYamlDescription;
					}

					if (frontmatter != null)
					{
						try
						{
							var metadataBuilder = ImmutableDictionary.CreateBuilder<SubAgentMetadataType, string>();
							var additionalMetadataBuilder = ImmutableDictionary.CreateBuilder<string, string>();
							var allowedToolsBuilder = ImmutableList.CreateBuilder<ToolNameWithSpecifier>();
							var availableToolsBuilder = ImmutableList.CreateBuilder<ToolNameWithSpecifier>();
							var disallowedToolsBuilder = ImmutableList.CreateBuilder<ToolNameWithSpecifier>();
							var skillsBuilder = ImmutableList.CreateBuilder<string>();
							var subAgentsBuilder = ImmutableList.CreateBuilder<string>();
							var memoryBlocksBuilder = ImmutableDictionary.CreateBuilder<string, MemoryBlockAttachmentMode>();
							var tagsBuilder = ImmutableList.CreateBuilder<string>();

							var additionalPropertiesBuilder = ImmutableDictionary.CreateBuilder<string, YamlNode>();

							if (!string.IsNullOrEmpty(frontmatter.Compatibility))
								metadataBuilder.Add(SubAgentMetadataType.Compatibility, frontmatter.Compatibility);
							if (!string.IsNullOrEmpty(frontmatter.License))
								metadataBuilder.Add(SubAgentMetadataType.License, frontmatter.License);

							foreach (var (key, value) in frontmatter.Metadata ?? [])
							{
								switch (key)
								{
									case "author":
										metadataBuilder.Add(SubAgentMetadataType.Author, value);
										break;
									case "version":
										metadataBuilder.Add(SubAgentMetadataType.Version, value);
										break;
									default:
										additionalMetadataBuilder.Add(key, value);
										break;
								}
							}

							static void ParseToolList(YamlNode rootNode, ref ImmutableList<ToolNameWithSpecifier>.Builder builder)
							{
								if (rootNode is YamlScalarNode toolsStrNode)
								{
									foreach (var tool in ToolNameWithSpecifierParser.FindAllMatches(toolsStrNode.Value ?? ""))
									{
										builder.Add(tool);
									}
								}
								else if (rootNode is YamlSequenceNode toolsSeqNode)
								{
									foreach (var node in toolsSeqNode)
									{
										if (node is YamlScalarNode s && !string.IsNullOrWhiteSpace(s.Value))
											foreach (var tool in ToolNameWithSpecifierParser.FindAllMatches(s.Value))
											{
												builder.Add(tool);
											}
									}
								}
							}

							if (frontmatterMap!.Children.TryGetValue("allowed-tools", out var allowedTools))
							{
								ParseToolList(allowedTools, ref allowedToolsBuilder);
							}

							if (frontmatterMap!.Children.TryGetValue("available-tools", out var availableTools))
							{
								ParseToolList(availableTools, ref availableToolsBuilder);
							}

							if (frontmatterMap!.Children.TryGetValue("disallowed-tools", out var disallowedTools))
							{
								ParseToolList(disallowedTools, ref disallowedToolsBuilder);
							}

							static void ParseStringList(YamlNode rootNode, ref ImmutableList<string>.Builder builder)
							{
								if (rootNode is YamlScalarNode scalarNode)
								{
									if (!string.IsNullOrWhiteSpace(scalarNode.Value))
										builder.Add(scalarNode.Value.Trim());
								}
								else if (rootNode is YamlSequenceNode seqNode)
								{
									foreach (var node in seqNode)
									{
										if (node is YamlScalarNode s && !string.IsNullOrWhiteSpace(s.Value))
											builder.Add(s.Value.Trim());
									}
								}
							}

							static void ParseMemoryBlocks(YamlNode rootNode, ref ImmutableDictionary<string, MemoryBlockAttachmentMode>.Builder builder)
							{
								if (rootNode is YamlMappingNode mappingNode)
								{
									foreach (var (key, value) in mappingNode.Children)
									{
										var blockName = key.ToString().Trim();
										if (blockName.Length == 0)
											continue;

										var mode = MemoryBlockAttachmentMode.Standard;
										if (value is YamlScalarNode modeNode &&
											Enum.TryParse<MemoryBlockAttachmentMode>(modeNode.Value, ignoreCase: true, out var parsed))
										{
											mode = parsed;
										}
										builder[blockName] = mode;
									}
								}
								else if (rootNode is YamlSequenceNode seqNode)
								{
									foreach (var node in seqNode)
									{
										if (node is YamlScalarNode s && !string.IsNullOrWhiteSpace(s.Value))
											builder[s.Value.Trim()] = MemoryBlockAttachmentMode.Standard;
									}
								}
								else if (rootNode is YamlScalarNode scalarNode && !string.IsNullOrWhiteSpace(scalarNode.Value))
								{
									builder[scalarNode.Value.Trim()] = MemoryBlockAttachmentMode.Standard;
								}
							}

							if (frontmatterMap!.Children.TryGetValue("skills", out var skillsNode))
							{
								ParseStringList(skillsNode, ref skillsBuilder);
							}

							if (frontmatterMap!.Children.TryGetValue("sub-agents", out var subAgentsNode))
							{
								ParseStringList(subAgentsNode, ref subAgentsBuilder);
							}

							if (frontmatterMap!.Children.TryGetValue("memory-blocks", out var memoryBlocksNode))
							{
								ParseMemoryBlocks(memoryBlocksNode, ref memoryBlocksBuilder);
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
										case "model":
										case "allowed-tools":
										case "available-tools":
										case "disallowed-tools":
										case "metadata":
										case "skills":
										case "sub-agents":
										case "memory-blocks":
										case "tags":
											continue;

										default:
											additionalPropertiesBuilder.Add(strKey, value);
											break;
									}
								}
							}

							return new SubAgentInfo
							{
								Name = name.Trim(),
								Description = description.Trim(),
								SystemPromptGetter = _ => prompt,
								Source = source,
								Path = fullpath,
								Metadata = metadataBuilder.ToImmutableDictionary(),
								AdditionalMetadata = additionalMetadataBuilder.ToImmutableDictionary(),
								AllowedTools = allowedToolsBuilder.ToImmutableList(),
								AvailableTools = availableToolsBuilder.ToImmutableList(),
								DisallowedTools = disallowedToolsBuilder.ToImmutableList(),
								Skills = skillsBuilder.ToImmutableList(),
								SubAgents = subAgentsBuilder.ToImmutableList(),
								MemoryBlocks = memoryBlocksBuilder.ToImmutableDictionary(),
								Tags = tagsBuilder.ToImmutableList(),
								AdditionalProperties = additionalPropertiesBuilder.ToImmutableDictionary(),
								Model = frontmatter.Model,
								Diagnostic = diagnosticCodes != SubAgentDiagnosticCode.None ? new SubAgentDiagnostic
								{
									IsFatal = false,
									Codes = diagnosticCodes,
									Exception = exception
								} : null
							};
						}
						catch (Exception ex)
						{
							return new SubAgentInfo
							{
								Name = name.Trim(),
								Description = description.Trim(),
								SystemPromptGetter = _ => prompt,
								Source = source,
								Path = fullpath,
								Diagnostic = new SubAgentDiagnostic
								{
									IsFatal = false,
									Codes = diagnosticCodes | SubAgentDiagnosticCode.YamlDecodingError,
									Exception = ex
								}
							};
						}
					}

					return new SubAgentInfo
					{
						Name = name.Trim(),
						Description = description.Trim(),
						SystemPromptGetter = _ => prompt,
						Source = source,
						Path = fullpath,
						Diagnostic = new SubAgentDiagnostic
						{
							IsFatal = false,
							Codes = diagnosticCodes,
							Exception = exception
						}
					};
				});

			_parser = builder.Build();
		}

		public SubAgentInfo Parse(string fullpath, string contents, SubAgentSource source = SubAgentSource.Unknown)
		{
			return _parser.Parse<SubAgentInfo>(contents, parameter: new ParserParameter
			{
				FullPath = fullpath,
				Source = source
			});
		}
	}
}
