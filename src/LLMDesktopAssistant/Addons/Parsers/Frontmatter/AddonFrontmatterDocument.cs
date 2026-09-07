using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using LLMDesktopAssistant.StructuredValues;
using LLMDesktopAssistant.StructuredValues.Const;
using LLMDesktopAssistant.StructuredValues.Converters;
using LLMDesktopAssistant.Tools;

namespace LLMDesktopAssistant.Addons.Parsers.Frontmatter
{
	public class AddonFrontmatterDocument
	{
		private readonly ConstNodeDictionaryValue _frontmatter;

		private readonly HashSet<string> _visitedKeys = [];

		public AddonFrontmatterDocument(INodeValue frontmatter)
		{
			_frontmatter = (frontmatter.ToConstNodeValue() ?? throw new ArgumentNullException(nameof(frontmatter)))
				as ConstNodeDictionaryValue ?? throw new ArgumentException("Frontmatter must be a dictionary", nameof(frontmatter));
		}

		public static AddonFrontmatterPropertyParser<T> FindParser<T>()
		{
			if (typeof(T) == typeof(string))
			{
				return (AddonFrontmatterPropertyParser<T>)(object)AddonFrontmatterStringPropertyParser.Instance;
			}

			if (typeof(T) == typeof(bool))
			{
				return (AddonFrontmatterPropertyParser<T>)(object)AddonFrontmatterBooleanPropertyParser.Instance;
			}

			if (typeof(T) == typeof(int))
			{
				return (AddonFrontmatterPropertyParser<T>)(object)AddonFrontmatterIntegerPropertyParser.Instance;
			}

			if (typeof(T) == typeof(ImmutableList<string>))
			{
				return (AddonFrontmatterPropertyParser<T>)(object)AddonFrontmatterStringListPropertyParser.Instance;
			}

			if (typeof(T) == typeof(ImmutableDictionary<string, string>))
			{
				return (AddonFrontmatterPropertyParser<T>)(object)AddonFrontmatterStringDictionaryPropertyParser.Instance;
			}

			if (typeof(T) == typeof(ImmutableList<ToolNameWithSpecifier>))
			{
				return (AddonFrontmatterPropertyParser<T>)(object)AddonFrontmatterToolListPropertyParser.Instance;
			}

			if (typeof(T) == typeof(JsonObject))
			{
				return (AddonFrontmatterPropertyParser<T>)(object)AddonFrontmatterJsonObjectPropertyParser.Instance;
			}

			if (typeof(T).IsAssignableTo(typeof(INodeValue)))
			{
				return new AddonFrontmatterPropertyNodeParser<T>();
			}

			throw new NotSupportedException($"Parser for type {typeof(T)} is not supported");
		}

		public bool TryGet<T>(string key, [NotNullWhen(true)] out T result, AddonFrontmatterPropertyParser<T>? parser = null)
		{
			parser ??= FindParser<T>();
			if (_frontmatter.Items.TryGetValue(key, out var item))
			{
				_visitedKeys.Add(key);
				AddonDiagnostic? diagnostic = null;
				if (parser.TryParse(item, ref diagnostic, false, out result))
					return true;
			}
			result = default!;
			return false;
		}

		public T Get<T>(string key, T defaultValue = default!, AddonFrontmatterPropertyParser<T>? parser = null)
		{
			parser ??= FindParser<T>();
			if (_frontmatter.Items.TryGetValue(key, out var item))
			{
				_visitedKeys.Add(key);
				AddonDiagnostic? diagnostic = null;
				if (parser.TryParse(item, ref diagnostic, false, out var result))
					return result;
			}
			return defaultValue;
		}

		public T Request<T>(string key, ref AddonDiagnostic? diagnostic, T defaultValue = default!,
			AddonDiagnosticCodeValue missingCode = default, AddonFrontmatterPropertyParser<T>? parser = null)
		{
			parser ??= FindParser<T>();
			if (_frontmatter.Items.TryGetValue(key, out var item))
			{
				_visitedKeys.Add(key);
				if (parser.TryParse(item, ref diagnostic, false, out var result))
					return result;
			}
			else
			{
				diagnostic = diagnostic.Combine(new AddonDiagnostic
				{
					IsFatal = false,
					Codes = missingCode
				});
			}
			return defaultValue;
		}

		public T Require<T>(string key, ref AddonDiagnostic? diagnostic, T defaultValue = default!,
			AddonDiagnosticCodeValue missingCode = default, AddonFrontmatterPropertyParser<T>? parser = null)
		{
			parser ??= FindParser<T>();
			if (_frontmatter.Items.TryGetValue(key, out var item))
			{
				_visitedKeys.Add(key);
				if (parser.TryParse(item, ref diagnostic, true, out var result))
					return result;
			}
			else
			{
				diagnostic = diagnostic.Combine(new AddonDiagnostic
				{
					IsFatal = true,
					Codes = missingCode
				});
			}
			return defaultValue;
		}

		public AddonMetadata GetMetadata(ref AddonDiagnostic? diagnostic, AddonPackInfo? packInfo)
		{
			var metadataBuilder = ImmutableDictionary.CreateBuilder<AddonMetadataType, string>();
			var additionalMetadataBuilder = ImmutableDictionary.CreateBuilder<string, string>();
			var tagsBuilder = ImmutableHashSet.CreateBuilder<string>();

			var compatibility = Get<string>("compatibility");
			if (!string.IsNullOrEmpty(compatibility))
				metadataBuilder.Add(AddonMetadataType.Compatibility, compatibility);
			var license = Get<string>("license");
			if (!string.IsNullOrEmpty(license))
				metadataBuilder.Add(AddonMetadataType.License, license);

			foreach (var (key, value) in Get<ConstNodeDictionaryValue>("metadata")?.Items ?? [])
			{
				switch (key)
				{
					case "author":
						var author = value.AsString();
						if (!string.IsNullOrEmpty(author))
							metadataBuilder.Add(AddonMetadataType.Author, author);
						else
							diagnostic = diagnostic.Combine(new AddonDiagnostic
							{
								Messages = ["Author metadata is not a string"]
							});
						break;

					case "version":
						var version = value.AsString();
						if (!string.IsNullOrEmpty(version))
							metadataBuilder.Add(AddonMetadataType.Version, version);
						else
							diagnostic = diagnostic.Combine(new AddonDiagnostic
							{
								Messages = ["Version metadata is not a string"]
							});
						break;

					default:
						var additionalMetadata = value.AsString();
						if (!string.IsNullOrEmpty(additionalMetadata))
							additionalMetadataBuilder.Add(key, additionalMetadata);
						else
							diagnostic = diagnostic.Combine(new AddonDiagnostic
							{
								Messages = [$"'{key}' metadata is not a string"]
							});
						break;
				}
			}

			var tags = Get<ConstNodeValue>("tags");
			if (tags is ConstNodeArrayValue tagsArray)
			{
				foreach (var tag in tagsArray.Items)
				{
					foreach (var splitTag in tag.AsString()?
						.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [])
					{
						tagsBuilder.Add(splitTag);
					}
				}
			}
			else if (tags is ConstNodeStringValue tagsString)
			{
				foreach (var splitTag in tagsString.Value?
					.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [])
				{
					tagsBuilder.Add(splitTag);
				}
			}

			if (packInfo != null)
			{
				foreach (var (key, value) in packInfo.Metadata)
					metadataBuilder.TryAdd(key, value);

				foreach (var (key, value) in packInfo.AdditionalMetadata)
					additionalMetadataBuilder.TryAdd(key, value);

				foreach (var tag in packInfo.Tags)
					tagsBuilder.Add(tag);
			}

			return new AddonMetadata
			{
				Metadata = metadataBuilder.ToImmutable(),
				AdditionalMetadata = additionalMetadataBuilder.ToImmutable(),
				Tags = tagsBuilder.ToImmutable()
			};
		}

		/// <summary>
		/// Returns all additional properties that were not visited.
		/// </summary>
		public ImmutableDictionary<string, ConstNodeValue> GetAdditionaProperties()
		{
			var result = ImmutableDictionary.CreateBuilder<string, ConstNodeValue>();

			foreach (var (key, value) in _frontmatter.Items)
			{
				if (!_visitedKeys.Contains(key))
				{
					result.Add(key, value);
				}
			}

			return result.ToImmutable();
		}
	}
}
