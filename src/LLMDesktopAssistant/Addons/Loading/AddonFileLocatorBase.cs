using System.Runtime.InteropServices;

namespace LLMDesktopAssistant.Addons.Loading
{
	public abstract class AddonFileLocatorBase<T> : IAddonFileLocator<T>
	{
		/// <summary>
		/// Gets the folder names to search for addons.
		/// Examples: 'skills', 'agents', 'tools'.
		/// </summary>
		protected abstract string[] Folders { get; }

		/// <summary>
		/// Gets the file extensions to search for addons (with or without a dot).
		/// Examples: '.md', '.txt', '.llt', '.hbs', '.lua', '.py'.
		/// </summary>
		/// <remarks>
		/// IMPORTANT: These extensions should be placed in a specific order based on their priority.
		/// First extensions have higher priority than later ones.
		/// </remarks>
		protected abstract string[] Extensions { get; }

		/// <summary>
		/// Gets whether to allow short format names for addons.
		/// </summary>
		protected abstract bool AllowShortFormat { get; }

		/// <summary>
		/// Gets the full format name of the addon file without extension.
		/// If provided, the locator will try to search for '.agents/folder_name/addon_name/FORMAT_NAME.ext'.
		/// If not provided, the locator won't search for a full format.
		/// Examples: 'SKILL' (for SKILL.md or SKILL.mdx), 'AGENT', 'BLOCK'
		/// </summary>
		protected abstract string? FullFormatName { get; }

		public IEnumerable<AddonPathInfo> LocateFiles(AddonFileLocatorConfiguration config)
		{
			var folders = new List<AddonPathInfo>();
			var notExistingFolders = new List<AddonPathInfo>();
			foreach (var pack in config.PackPaths ?? [])
			{
				if (Directory.Exists(pack.Path))
					foreach (var folder in Folders)
					{
						var folderPath = Path.Combine(pack.Path, folder);
						if (Directory.Exists(folderPath))
							folders.Add(new AddonPathInfo(folderPath, null, pack.SourcePack));
					}
				else
					notExistingFolders.Add(pack);
			}

			foreach (var folder in config.FolderPaths ?? [])
			{
				if (Directory.Exists(folder.Path))
					folders.Add(folder);
				else
					notExistingFolders.Add(folder);
			}

			var files = new List<AddonPathInfo>();

			// key = extension without dot, value = priority (higher number means higher priority)
			var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
			var extensions = new Dictionary<string, int>(comparer);
			var prioritizedPaths = new Dictionary<string, (int, AddonPathInfo)>(comparer);
			for (int i = 0; i < Extensions.Length; i++)
			{
				var ext = Extensions[i].TrimStart('.');
				extensions[ext] = Extensions.Length - i;
			}

			void TryAddFile(string file, bool isShortForm, AddonPackInfo? sourcePack)
			{
				var ext = Path.GetExtension(file)?.TrimStart('.') ?? string.Empty;
				if (extensions.TryGetValue(ext, out var priority))
				{
					var purePath = file[..^ext.Length].TrimEnd('.');
					if (!prioritizedPaths.TryGetValue(purePath, out var entry) || entry.Item1 < priority)
						prioritizedPaths[purePath] = (priority, new AddonPathInfo(file, isShortForm, sourcePack));
				}
			}

			foreach (var folder in folders)
			{
				if (FullFormatName != null)
				{
					foreach (var directory in Directory.GetDirectories(folder.Path))
					{
						foreach (var file in Directory.GetFiles(directory, $"{FullFormatName}.*", SearchOption.TopDirectoryOnly))
						{
							TryAddFile(file, false, folder.SourcePack);
						}
					}
				}
				if (AllowShortFormat)
				{
					foreach (var file in Directory.GetFiles(folder.Path))
					{
						TryAddFile(file, true, folder.SourcePack);
					}
				}
			}

			foreach (var entry in prioritizedPaths)
				files.Add(entry.Value.Item2);

			files.AddRange(notExistingFolders);
			files.AddRange(config.AddonFiles ?? []);

			return files;
		}
	}
}
