using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using LLMDesktopAssistant.LLM.Settings;

namespace LLMDesktopAssistant.Addons.Loading
{
	/// <summary>
	/// Base class for locating addon packs.
	/// </summary>
	public abstract class AddonPackLocatorBase : IAddonPackLocator
	{
		/// <inheritdoc/>
		public abstract IEnumerable<AddonPackInfo> GetAllPacks();

		/// <inheritdoc/>
		public IEnumerable<AddonPackInfo> GetEffectivePacks()
		{
			var appconfig = GetEffectiveSettings();
			var result = new List<AddonPackInfo>();

			foreach (var pack in GetAllPacks())
			{
				if (pack.IsConfigurable && appconfig != null)
				{
					if (appconfig.EnabledPacks.TryGetValue(pack.Path, out var enabled))
					{
						if (enabled)
							result.Add(pack);
					}
					else
					{
						if (appconfig.EnablePacksByDefault)
							result.Add(pack);
					}
				}
				else
				{
					result.Add(pack);
				}
			}

			return result;
		}

		protected AddonPackInfo ParsePack(string packDirectoryPath, AddonPackSource source, bool isConfigurable)
		{
			string defaultName = Path.GetFileName(packDirectoryPath.TrimEnd('\\', '/'));
			string manifestPath = Path.Combine(packDirectoryPath, "pack.json");

			if (File.Exists(manifestPath))
			{
				try
				{
					var contents = File.ReadAllText(manifestPath);
					var manifest = JsonNode.Parse(contents);

					string? name = null, description = null;
					var metadataBuilder = ImmutableDictionary.CreateBuilder<AddonMetadataType, string>();
					var additionalMetadataBuilder = ImmutableDictionary.CreateBuilder<string, string>();
					var tagsBuilder = ImmutableHashSet.CreateBuilder<string>();

					foreach (var (key, value) in manifest as JsonObject ?? [])
					{
						if (key is "tags")
						{
							if (value is JsonArray tagsArray)
							{
								foreach (var tag in tagsArray)
								{
									if (tag?.GetValueKind() is JsonValueKind.String)
									{
										var tagStr = tag.GetValue<string>();
										if (!string.IsNullOrWhiteSpace(tagStr))
											tagsBuilder.Add(tagStr);
									}
								}
							}
							continue;
						}

						string? valueStr = value?.GetValueKind() switch
						{
							JsonValueKind.Number => value.GetValue<double>().ToString(CultureInfo.InvariantCulture),
							JsonValueKind.String => value.GetValue<string>(),
							_ => null
						};
						if (string.IsNullOrWhiteSpace(valueStr))
							continue;

						switch (key)
						{
							case "name":
								name = valueStr;
								break;

							case "description":
								description = valueStr;
								break;

							case "author":
								metadataBuilder.Add(AddonMetadataType.Author, valueStr);
								break;

							case "version":
								metadataBuilder.Add(AddonMetadataType.Version, valueStr);
								break;

							case "compatibility":
								metadataBuilder.Add(AddonMetadataType.Compatibility, valueStr);
								break;

							case "license":
								metadataBuilder.Add(AddonMetadataType.License, valueStr);
								break;

							default:
								additionalMetadataBuilder.Add(key, valueStr);
								break;
						}
					}

					return new AddonPackInfo
					{
						Name = name ?? defaultName,
						Description = description,
						Path = packDirectoryPath,
						Source = source,
						IsConfigurable = isConfigurable,

						IsManifestValid = true,
						Metadata = metadataBuilder.ToImmutable(),
						AdditionalMetadata = additionalMetadataBuilder.ToImmutable(),
						Tags = tagsBuilder.ToImmutable()
					};
				}
				catch
				{
					return new AddonPackInfo
					{
						Name = defaultName,
						Path = packDirectoryPath,
						Source = source,
						IsConfigurable = isConfigurable,
						IsManifestValid = false
					};
				}
			}

			return new AddonPackInfo
			{
				Name = defaultName,
				Path = packDirectoryPath,
				Source = source,
				IsConfigurable = isConfigurable,
				IsManifestValid = null
			};
		}

		/// <summary>
		/// Gets the effective settings for filtering effective enabled packs.
		/// </summary>
		protected abstract AddonPacksSettings? GetEffectiveSettings();
	}
}
