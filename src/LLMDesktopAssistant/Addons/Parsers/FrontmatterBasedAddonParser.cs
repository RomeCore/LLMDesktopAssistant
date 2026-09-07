using System.Collections.Concurrent;
using LLMDesktopAssistant.Addons.Parsers.Frontmatter;
using LLMDesktopAssistant.StructuredValues.Converters;
using RCParsing;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace LLMDesktopAssistant.Addons.Parsers
{
	public abstract class FrontmatterBasedAddonParser<T> : IAddonFileParser<T>
		where T : AddonBase<T>, new()
	{
		protected class AddonParserDescriptor : IEquatable<AddonParserDescriptor>
		{
			/// <summary>
			/// The sequence of characters that marks the start of the frontmatter.
			/// Examples: '---' (Markdown), '"""' (Python), '--[[' (Lua).
			/// </summary>
			public required string FrontmatterStart { get; init; }

			/// <summary>
			/// The sequence of characters that marks the end of the frontmatter.
			/// Examples: '---' (Markdown), '"""' (Python), ']]' (Lua).
			/// </summary>
			public required string FrontmatterEnd { get; init; }

			/// <summary>
			/// Whether the parser requires frontmatter to be present in the file.
			/// </summary>
			public bool RequiresFrontmatter { get; init; }

			/// <summary>
			/// Whether to fetch name and description from markdown if cannot fetch them from frontmatter.
			/// </summary>
			public bool UseMarkdownFallback { get; init; }

			public override bool Equals(object? obj)
			{
				return obj is AddonParserDescriptor descriptor && Equals(descriptor);
			}

			public bool Equals(AddonParserDescriptor? other)
			{
				return other != null
					&& FrontmatterStart == other.FrontmatterStart && FrontmatterEnd == other.FrontmatterEnd
					&& RequiresFrontmatter == other.RequiresFrontmatter
					&& UseMarkdownFallback == other.UseMarkdownFallback;
			}

			public override int GetHashCode()
			{
				return HashCode.Combine(FrontmatterStart, FrontmatterEnd, RequiresFrontmatter, UseMarkdownFallback);
			}
		}

		protected class FrontmatteredAddonDocument
		{
			/// <summary>
			/// The frontmatter content of the document.
			/// </summary>
			public required string? Frontmatter { get; init; }

			/// <summary>
			/// The required frontmatter content of the document.
			/// This property will throw an exception if the frontmatter is not present.
			/// </summary>
			public string RequiredFrontmatter => Frontmatter ?? throw new InvalidOperationException("Frontmatter is required but not present.");

			/// <summary>
			/// The body content of the document. This is the content after the frontmatter.
			/// </summary>
			public required string Body { get; init; }

			/// <summary>
			/// The fallback name that fetched from body content.
			/// </summary>
			public string? FallbackName { get; init; }

			/// <summary>
			/// The fallback description that fetched from body content.
			/// </summary>
			public string? FallbackDescription { get; init; }
		}

		private static readonly ConcurrentDictionary<AddonParserDescriptor, Parser> _parserCache = [];

		private static readonly IDeserializer _yamlDeserializer = new DeserializerBuilder()
			.IgnoreUnmatchedProperties()
			.Build();

		private static Parser GetParser(AddonParserDescriptor descriptor)
		{
			var builder = new ParserBuilder();

			var ruleBuilder = builder.CreateMainRule();

			if (descriptor.RequiresFrontmatter)
			{
				ruleBuilder
					.Rule(b => b
						.Literal(descriptor.FrontmatterStart)
						.TextUntil(descriptor.FrontmatterEnd).Label("frontmatter")
						.Literal(descriptor.FrontmatterEnd)
						.Transform(v => v["frontmatter"].Text)
					).Label("frontmatter");
			}
			else
			{
				ruleBuilder
					.Optional(b => b
						.Literal(descriptor.FrontmatterStart)
						.TextUntil(descriptor.FrontmatterEnd).Label("frontmatter")
						.Literal(descriptor.FrontmatterEnd)
						.Transform(v => v["frontmatter"].Text)
					).Label("frontmatter");
			}

			if (descriptor.UseMarkdownFallback)
			{
				ruleBuilder
					.Optional(b => b
						.Literal("#").TextUntil("\n", "\r", "\r\n").Optional(b => b.Whitespaces())
						.Transform(v => v[1].Text)
					).Label("name")

					.Optional(b => b
						.OneOrMoreSeparated(b => b.TextUntil("\n", "\r", "\r\n"), s => s.Newline()).Optional(b => b.Whitespaces())
						.Transform(v => v[0].Text)
					).Label("desc");
			}

			ruleBuilder
				.AllText()
				
				.Transform(v =>
				{
					var frontmatter = v.TryGetValue<string>("frontmatter");
					string body = v.Span[v["frontmatter"].EndIndex..].Trim().ToString();
					var fallbackName = v.TryGetValue<string>("name");
					var fallbackDesc = v.TryGetValue<string>("desc");

					return new FrontmatteredAddonDocument
					{
						Frontmatter = frontmatter,
						Body = body,
						FallbackName = fallbackName,
						FallbackDescription = fallbackDesc
					};
				});

			return builder.Build();
		}

		public IEnumerable<T> Parse(string content, AddonPathInfo fileInfo)
		{
			var descriptor = GetDescriptorFor(content, fileInfo);
			var parser = _parserCache.GetOrAdd(descriptor, GetParser);
			var result = new T();

			result.Path ??= fileInfo.Path;
			result.SourcePack ??= fileInfo.SourcePack;

			var fallbackName = GetFallbackName(fileInfo);

			FrontmatteredAddonDocument document;
			try
			{
				document = parser.Parse<FrontmatteredAddonDocument>(content);
			}
			catch (Exception ex)
			{
				result.Name = fallbackName;
				result.Description = string.Empty;
				result.Diagnostic = new AddonDiagnostic
				{
					IsFatal = true,
					Codes = AddonDiagnosticCode.GeneralParsingError,
					Exceptions = [ex]
				};
				return Finish(result);
			}

			result.BodyGetter = _ => document.Body;

			var diagnostic = (AddonDiagnostic?)null;

			var hasFrontmatter = !string.IsNullOrWhiteSpace(document.Frontmatter);

			if (descriptor.RequiresFrontmatter && !hasFrontmatter)
			{
				diagnostic = diagnostic.Combine(new AddonDiagnostic
				{
					IsFatal = true,
					Codes = AddonDiagnosticCode.MissingFrontmatter
				});
			}

			AddonFrontmatterDocument? frontmatterDocument = null;
			if (hasFrontmatter)
			{
				try
				{
					var yamlRoot = _yamlDeserializer.Deserialize<YamlMappingNode>(document.Frontmatter!);
					if (yamlRoot is not null)
						frontmatterDocument = new AddonFrontmatterDocument(yamlRoot.ToConstNodeValue()!);
				}
				catch (Exception ex)
				{
					diagnostic = diagnostic.Combine(new AddonDiagnostic
					{
						IsFatal = false,
						Codes = AddonDiagnosticCode.FrontmatterParsingError,
						Exceptions = [ex]
					});
				}
			}

			if (frontmatterDocument is not null)
			{
				try
				{
					Populate(result, frontmatterDocument, ref diagnostic);
				}
				catch (Exception ex)
				{
					diagnostic = diagnostic.Combine(new AddonDiagnostic
					{
						IsFatal = true,
						Codes = AddonDiagnosticCode.FrontmatterDecodingError,
						Exceptions = [ex]
					});
				}

				// Apply common metadata (compatibility, license, metadata, tags) and pack info.
				var metadata = frontmatterDocument.GetMetadata(ref diagnostic, fileInfo.SourcePack);
				result.Metadata = metadata.Metadata;
				result.AdditionalMetadata = metadata.AdditionalMetadata;
				result.Tags = metadata.Tags;

				// Apply common name and description from frontmatter.
				var fmName = frontmatterDocument.Get<string>("name");
				if (!string.IsNullOrWhiteSpace(fmName))
				{
					result.Name = fmName.Trim();
				}
				else
				{
					diagnostic = diagnostic.Combine(new AddonDiagnostic
					{
						Codes = AddonDiagnosticCode.MissingFrontmatterName
					});
					if (!string.IsNullOrWhiteSpace(document.FallbackName))
						result.Name = document.FallbackName;
					else if (!string.IsNullOrWhiteSpace(fallbackName))
						result.Name = fallbackName;
				}

				var fmDescription = frontmatterDocument.Get<string>("description");
				if (!string.IsNullOrWhiteSpace(fmDescription))
				{
					result.Description = fmDescription.Trim();
				}
				else
				{
					diagnostic = diagnostic.Combine(new AddonDiagnostic
					{
						Codes = AddonDiagnosticCode.MissingFrontmatterDescription
					});
					if (!string.IsNullOrWhiteSpace(document.FallbackDescription))
						result.Description = document.FallbackDescription;
					else
						result.Description = string.Empty;
				}
			}
			else
			{
				if (!string.IsNullOrWhiteSpace(document.FallbackName))
					result.Name = document.FallbackName;
				else if (!string.IsNullOrWhiteSpace(fallbackName))
					result.Name = fallbackName;

				if (!string.IsNullOrWhiteSpace(document.FallbackDescription))
					result.Description = document.FallbackDescription;
				else
					result.Description = string.Empty;
			}

			if (string.IsNullOrWhiteSpace(result.Name))
			{
				diagnostic = diagnostic.Combine(new AddonDiagnostic
				{
					Codes = AddonDiagnosticCode.MissingName
				});
				result.Name = "unknown";
			}

			if (!IsValidName(result.Name))
			{
				result.Name = NormalizeName(result.Name);
				diagnostic = diagnostic.Combine(new AddonDiagnostic
				{
					Codes = AddonDiagnosticCode.NameFormatError
				});
			}

			if (!IsNameMatchingFile(result.Name, fileInfo))
			{
				diagnostic = diagnostic.Combine(new AddonDiagnostic
				{
					Codes = AddonDiagnosticCode.NameFSMismatch
				});
			}

			if (string.IsNullOrWhiteSpace(result.Description))
			{
				diagnostic = diagnostic.Combine(new AddonDiagnostic
				{
					Codes = AddonDiagnosticCode.MissingDescription
				});
				result.Description = string.Empty;
			}

			if (diagnostic is not null)
				result.Diagnostic = result.Diagnostic.Combine(diagnostic);

			return Finish(result);
		}

		private IEnumerable<T> Finish(T result)
		{
			result.Name ??= "unknown";
			result.Description ??= string.Empty;
			result.CheckRequiredProperties();
			result.Freeze();
			return [result];
		}

		/// <summary>
		/// Returns the fallback name for the addon based on its file path.
		/// </summary>
		protected virtual string GetFallbackName(AddonPathInfo fileInfo)
		{
			return fileInfo.IsShortForm ?? true
				? Path.GetFileName(fileInfo.Path)
				: Path.GetFileName(Path.GetDirectoryName(fileInfo.Path))!;
		}

		/// <summary>
		/// Validates the resulting addon name. Return false to fail with the <see cref="AddonDiagnosticCode.NameFormatError"/> code.
		/// </summary>
		protected virtual bool IsValidName(string name)
		{
			return true;
		}

		/// <summary>
		/// Normalizes an invalid name into a valid format.
		/// </summary>
		protected virtual string NormalizeName(string name)
		{
			return name;
		}

		/// <summary>
		/// Checks whether the given name matches the addon file name. Return false to fail with the
		/// <see cref="AddonDiagnosticCode.NameFSMismatch"/> code.
		/// </summary>
		protected virtual bool IsNameMatchingFile(string name, AddonPathInfo fileInfo)
		{
			return name.Equals(GetFallbackName(fileInfo), StringComparison.Ordinal);
		}

		/// <summary>
		/// Gets the parser descriptor (frontmatter markers, fallback settings) for the given file.
		/// </summary>
		protected abstract AddonParserDescriptor GetDescriptorFor(string content, AddonPathInfo fileInfo);

		/// <summary>
		/// Populates the addon with addon-specific properties from the already-parsed frontmatter.
		/// Note: the base class already fills <see cref="AddonBase{Self}.Name"/>, <see cref="AddonBase{Self}.Description"/>,
		/// <see cref="AddonBase{Self}.Path"/>, <see cref="AddonBase{Self}.SourcePack"/>, <see cref="AddonBase{Self}.Body"/>,
		/// <see cref="AddonMetadata.Metadata"/>, <see cref="AddonMetadata.AdditionalMetadata"/>
		/// and <see cref="AddonMetadata.Tags"/>.
		/// </summary>
		protected abstract void Populate(T addon, AddonFrontmatterDocument frontmatter, ref AddonDiagnostic? diagnostic);
	}
}
