using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Utils;
using LLMDesktopAssistant.Utils.Files;

namespace LLMDesktopAssistant.Addons.Loading
{
	/// <summary>
	/// <inheritdoc cref="IAddonPathImpactDetector"/>
	/// </summary>
	/// <remarks>
	/// <para>
	/// The detector works only on potential paths: the checked path is matched against the potential addon
	/// locations (effective working directories of the chat, addon search folders and explicitly configured
	/// pack paths) and the addon type layouts discovered from the registered <see cref="IAddonFileLocator"/>
	/// instances ('skills', 'agents', 'tools', 'templates', 'scripts/lua', ...). The detector never accesses
	/// the filesystem: the caller describes what happens with the path (<see cref="FileOperation"/> and the
	/// optional existing ancestor directory), and the detector applies the rules to the potential locations.
	/// </para>
	/// <para>
	/// For every potential pack root the following rules are applied:
	/// <list type="bullet">
	/// <item><description>The pack root itself, the 'packs' container and a whole pack directory impact
	/// <see cref="AddonKind.All"/>: the pack (or all packs) with addons of every kind inside it can appear
	/// or disappear together with the directory, so the result must not be narrowed to <see cref="AddonKind.Pack"/>.</description></item>
	/// <item><description>A path inside a pack ('packs/&lt;name&gt;/...') impacts <see cref="AddonKind.Pack"/>
	/// when the pack directory can be created by the operation, or when the 'pack.json' manifest is touched,
	/// combined with the addon kind addressed by the pack content layout.</description></item>
	/// <item><description>A whole pack root appears (<see cref="FileOperation.Create"/> with the root among
	/// the created directories) impacts <see cref="AddonKind.Pack"/> combined with the kind of the touched path.</description></item>
	/// <item><description>Anything inside an addon type folder impacts the corresponding kind, including the touch
	/// of the folder itself and of addon resources inside full format folders (for example, 'skills/my-skill/helper.py').</description></item>
	/// </list>
	/// </para>
	/// <para>
	/// The matching is intentionally extension-agnostic: being inside an addon type folder means that the path can
	/// be seen by the corresponding addon file locator, and new addon formats/extensions are supported automatically.
	/// </para>
	/// </remarks>
	[ChatService(typeof(IAddonPathImpactDetector))]
	public class AddonPathImpactDetector : IAddonPathImpactDetector
	{
		private const string PacksContainerFolderName = "packs";
		private const string PackManifestFileName = "pack.json";

		private static readonly StringComparison PathComparison = OperatingSystem.IsWindows()
			? StringComparison.OrdinalIgnoreCase
			: StringComparison.Ordinal;

		private static readonly StringComparer PathComparer = OperatingSystem.IsWindows()
			? StringComparer.OrdinalIgnoreCase
			: StringComparer.Ordinal;

		private static readonly char[] DirectorySeparators = ['\\', '/'];

		private readonly IAddonPackSearchFoldersProvider _searchFoldersProvider;
		private readonly IChatSettingsService _chatSettings;
		private readonly AddonPathLayout[] _layouts;

		public AddonPathImpactDetector(
			IAddonPackSearchFoldersProvider searchFoldersProvider,
			IChatSettingsService chatSettings,
			IEnumerable<IAddonTypeDescriptor> typeDescriptors,
			IEnumerable<IAddonFileLocator> fileLocators)
		{
			_searchFoldersProvider = searchFoldersProvider;
			_chatSettings = chatSettings;

			// Map the addon CLR type of every locator (e.g. SkillInfo) to the corresponding addon kind (e.g. Skill)
			// through the registered addon type descriptors, so the folder layouts are never duplicated manually.
			var kindByAddonType = new Dictionary<Type, AddonKind>();
			foreach (var descriptor in typeDescriptors)
				kindByAddonType[descriptor.ClrType] = descriptor.Kind;

			var layouts = new List<AddonPathLayout>();
			foreach (var locator in fileLocators)
			{
				var addonType = GetAddonType(locator.GetType());
				if (addonType is null || !kindByAddonType.TryGetValue(addonType, out var kind))
					continue;

				foreach (var folder in locator.Folders)
				{
					var segments = SplitPath(folder);
					if (segments.Length > 0)
						layouts.Add(new AddonPathLayout(kind, segments));
				}
			}

			_layouts = [.. layouts];
		}

		/// <inheritdoc/>
		public AddonKind Detect(string path, bool? isFile,
			FileOperation operation = FileOperation.Create,
			string? existingAncestorDirectory = null)
		{
			if (string.IsNullOrWhiteSpace(path))
				return AddonKind.None;

			var normalizedPath = TryNormalizePath(path);
			if (normalizedPath is null)
				return AddonKind.None;

			var pathSegments = SplitPath(normalizedPath);

			// Directories the operation can create: the path segments below the existing ancestor directory.
			// When the ancestor is unknown, the worst case is assumed: every directory of the path may be missing.
			var createdDirectories = operation == FileOperation.Create
				? BuildCreatedDirectories(pathSegments, isFile, TryGetAncestorSegments(existingAncestorDirectory))
				: null;

			var result = AddonKind.None;

			foreach (var root in GetPotentialPackRoots())
			{
				if (!TryGetRelativeSegments(root.Segments, pathSegments, out var relative))
					continue;

				var rootCreated = false;
				var packStructureCreated = false;

				if (createdDirectories is not null)
				{
					foreach (var createdDirectory in createdDirectories)
					{
						if (!TryGetRelativeSegments(root.Segments, createdDirectory, out var createdRelative))
							continue;

						if (createdRelative.Length == 0)
							rootCreated = true;
						else if (root.IsPackContainer && createdRelative.Length <= 2 &&
							string.Equals(createdRelative[0], PacksContainerFolderName, PathComparison))
							packStructureCreated = true;
					}
				}

				result |= DetectAgainstPackRoot(relative, root, isFile, operation, rootCreated, packStructureCreated);
			}

			return result;
		}

		private AddonKind DetectAgainstPackRoot(string[] relative, PackRoot root, bool? isFile,
			FileOperation operation, bool rootCreated, bool packStructureCreated)
		{
			if (relative.Length == 0)
			{
				// The pack root itself is touched (created, deleted or renamed): addons of every type can appear
				// or disappear together with it, so all kinds (including the pack itself) are impacted.
				return isFile == true ? AddonKind.None : AddonKind.All;
			}

			// Unknown file/directory is treated as a directory to stay conservative.
			var isDirectory = isFile != true;

			if (root.IsPackContainer && string.Equals(relative[0], PacksContainerFolderName, PathComparison))
			{
				if (relative.Length <= 2)
				{
					// The 'packs' container itself or a whole pack directory: a pack (or all packs) can appear or
					// disappear entirely together with its content, so every addon kind living there is impacted.
					return isDirectory ? AddonKind.All : AddonKind.None;
				}

				// 'packs/<pack-name>/<content>': the addon layout is matched after the pack name.
				var packContent = relative.AsSpan(2);
				var kind = MatchAddonLayout(packContent, allowExactMatch: isDirectory);

				if (operation == FileOperation.Create)
				{
					// The pack directory (or the container) can be created implicitly together with the path.
					return (packStructureCreated || IsPackManifest(packContent) ? AddonKind.Pack : AddonKind.None) | kind;
				}

				if (IsPackManifest(packContent))
					return AddonKind.Pack;

				return kind;
			}

			if (root.ParsesManifest && IsPackManifest(relative))
				return AddonKind.Pack;

			var layoutKind = MatchAddonLayout(relative, allowExactMatch: isDirectory);
			if (operation == FileOperation.Create && rootCreated)
			{
				// The whole pack root appears: the created pack contains the touched path, so the pack itself
				// (and the kind addressed by the touched path) is impacted.
				return layoutKind | AddonKind.Pack;
			}

			return layoutKind;
		}

		private AddonKind MatchAddonLayout(ReadOnlySpan<string> relative, bool allowExactMatch)
		{
			var result = AddonKind.None;

			foreach (var layout in _layouts)
			{
				if (relative.Length < layout.Segments.Length)
					continue;
				if (!allowExactMatch && relative.Length == layout.Segments.Length)
					continue;

				var matches = true;
				for (int i = 0; i < layout.Segments.Length; i++)
				{
					if (!string.Equals(relative[i], layout.Segments[i], PathComparison))
					{
						matches = false;
						break;
					}
				}

				if (matches)
					result |= layout.Kind;
			}

			return result;
		}

		/// <summary>
		/// Enumerates the potential pack roots for the current chat: where addon packs can be located
		/// (mirrors the logic of the addon pack locators without any filesystem access).
		/// </summary>
		private List<PackRoot> GetPotentialPackRoots()
		{
			var settings = _chatSettings.Settings;

			var environmentWorkdirs = settings.Environment.GetEffectiveWorkingDirectories();
			var addonWorkdirs = settings.Addons.GetEffectiveWorkingDirectories();

			List<string> workingDirectories = addonWorkdirs.FetchFromAllWorkingDirectories
				? environmentWorkdirs.GetEnabledWorkingDirectories()
				: [environmentWorkdirs.GetWorkingDirectory()];

			var searchFolders = _searchFoldersProvider.GetSearchFolders()
				.Where(folder => !string.IsNullOrWhiteSpace(folder))
				.Distinct(PathComparer)
				.ToArray();

			var result = new List<PackRoot>();
			var seen = new HashSet<string>(PathComparer);

			// The application addons home (%LOCALAPPDATA%\.llmassist) is a fixed non-configurable pack
			// that contains 'skills', 'agents', 'scripts/lua', 'packs', etc.
			AddRoot(Directories.LocalAppData, isPackContainer: true, parsesManifest: false);

			// Global agent homes: %USERPROFILE%\.agents, %USERPROFILE%\.claude, ...
			foreach (var folder in searchFolders)
				AddRoot(Path.Combine(Directories.UserProfile, folder), isPackContainer: true, parsesManifest: false);

			foreach (var workdir in workingDirectories)
			{
				if (string.IsNullOrWhiteSpace(workdir))
					continue;

				// Working directory agent homes: <workdir>\.agents, <workdir>\.claude, ...
				foreach (var folder in searchFolders)
					AddRoot(Path.Combine(workdir, folder), isPackContainer: true, parsesManifest: false);

				// The working directory itself is a pack when the corresponding setting is enabled.
				// Such packs are synthetic: their 'packs' subfolder is not scanned, but the type folders are.
				if (addonWorkdirs.UseWorkingDirectoriesAsPacks)
					AddRoot(workdir, isPackContainer: false, parsesManifest: false);
			}

			// Explicitly configured packs: their manifests are parsed by the pack locators.
			foreach (var pack in settings.Addons.GetEffectiveAdditionalPackPaths())
				AddRoot(pack, isPackContainer: false, parsesManifest: true);

			return result;

			void AddRoot(string? root, bool isPackContainer, bool parsesManifest)
			{
				if (string.IsNullOrWhiteSpace(root))
					return;

				try
				{
					var normalized = Path.GetFullPath(root);
					var key = $"{normalized}\0{(isPackContainer ? 1 : 0)}{(parsesManifest ? 1 : 0)}";
					if (!seen.Add(key))
						return;
					result.Add(new PackRoot(SplitPath(normalized), isPackContainer, parsesManifest));
				}
				catch
				{
					// Ignore invalid configured paths.
				}
			}
		}

		/// <summary>
		/// Normalizes the path to an absolute form. Relative paths are resolved against the current effective
		/// working directory (mirrors <see cref="IWorkingDirectoryAccessService"/> behavior).
		/// </summary>
		private string? TryNormalizePath(string path)
		{
			try
			{
				if (Path.IsPathRooted(path))
					return Path.GetFullPath(path);

				var baseDirectory = _chatSettings.Settings.Environment.GetEffectiveWorkingDirectories().GetWorkingDirectory();
				if (string.IsNullOrWhiteSpace(baseDirectory))
					return null;

				return Path.GetFullPath(path, baseDirectory);
			}
			catch
			{
				// The path cannot be normalized (invalid characters etc.), so it cannot be an addon path.
				return null;
			}
		}

		private string[]? TryGetAncestorSegments(string? existingAncestorDirectory)
		{
			if (string.IsNullOrWhiteSpace(existingAncestorDirectory))
				return null;

			var normalized = TryNormalizePath(existingAncestorDirectory);
			return normalized is null ? null : SplitPath(normalized);
		}

		/// <summary>
		/// Gets the directories of the path that can be created by the operation: the segments below the existing
		/// ancestor directory. When the ancestor is unknown (or is not an ancestor of the path), the worst case
		/// is assumed: every directory of the path may not exist yet.
		/// </summary>
		private static List<string[]> BuildCreatedDirectories(string[] pathSegments, bool? isFile, string[]? ancestorSegments)
		{
			// For a file the last segment is the file name itself, so it cannot be a created directory.
			var directoryLength = isFile == true ? pathSegments.Length - 1 : pathSegments.Length;
			if (directoryLength <= 0)
				return [];

			var start = 0;
			if (ancestorSegments is not null && IsSegmentPrefix(ancestorSegments, pathSegments, directoryLength))
				start = ancestorSegments.Length;

			var result = new List<string[]>(directoryLength - start);
			for (int i = start + 1; i <= directoryLength; i++)
				result.Add(pathSegments[..i]);

			return result;
		}

		private static bool IsSegmentPrefix(string[] prefix, string[] segments, int length)
		{
			if (prefix.Length > length)
				return false;

			for (int i = 0; i < prefix.Length; i++)
			{
				if (!string.Equals(prefix[i], segments[i], PathComparison))
					return false;
			}

			return true;
		}

		private static bool TryGetRelativeSegments(string[] rootSegments, string[] pathSegments, out string[] relative)
		{
			relative = [];
			if (pathSegments.Length < rootSegments.Length)
				return false;

			for (int i = 0; i < rootSegments.Length; i++)
			{
				if (!string.Equals(pathSegments[i], rootSegments[i], PathComparison))
					return false;
			}

			relative = pathSegments[rootSegments.Length..];
			return true;
		}

		private static string[] SplitPath(string path)
		{
			return path.Split(DirectorySeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		}

		private static bool IsPackManifest(ReadOnlySpan<string> relative)
		{
			return relative.Length == 1 && string.Equals(relative[0], PackManifestFileName, PathComparison);
		}

		private static Type? GetAddonType(Type locatorType)
		{
			foreach (var interfaceType in locatorType.GetInterfaces())
			{
				if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IAddonFileLocator<>))
					return interfaceType.GetGenericArguments()[0];
			}

			return null;
		}

		/// <summary>
		/// Represents a potential addon pack location for the current chat.
		/// </summary>
		/// <param name="Segments">The path segments of the pack root.</param>
		/// <param name="IsPackContainer">Whether the 'packs' subfolder of this root contains scanned packs.</param>
		/// <param name="ParsesManifest">Whether the 'pack.json' manifest of this pack is parsed by the pack locators.</param>
		private readonly record struct PackRoot(string[] Segments, bool IsPackContainer, bool ParsesManifest);

		/// <summary>
		/// Represents an addon type layout: the folder segments (relative to a pack root) that belong
		/// to the addon kind, e.g. ['skills'] or ['scripts', 'lua'].
		/// </summary>
		private readonly record struct AddonPathLayout(AddonKind Kind, string[] Segments);
	}
}
