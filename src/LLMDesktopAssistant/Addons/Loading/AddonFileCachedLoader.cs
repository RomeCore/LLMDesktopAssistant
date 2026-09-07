using System.Collections.Concurrent;
using LLMDesktopAssistant.Addons.Parsers;
using LLMDesktopAssistant.Utils;
using Serilog;

namespace LLMDesktopAssistant.Addons.Loading
{
	public class AddonFileCachedLoader<T>(
		IAddonFileParser<T> parser,
		IDiagnosticAddonFactory<T> diagnosticFactory
	) : IReactiveAddonLoader<T>
	{
		private class CacheEntry
		{
			public required DateTime LastWriteTime { get; init; }
			public required long FileSize { get; init; }
			public required ImmutableList<T> Addons { get; init; }
		}

		private static readonly StringComparer _pathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
		private readonly RangeObservableCollection<T> _addons = [];
		private readonly ConcurrentDictionary<string, CacheEntry> _cache = new(_pathComparer);
		private readonly Lock _lock = new();

		/// <summary>
		/// Gets the reactive collection of addons. This collection is read-only.
		/// </summary>
		public ReadOnlyObservableCollection<T> Addons => field ??= new ReadOnlyObservableCollection<T>(_addons);

		public void Reload(IEnumerable<AddonPathInfo> files)
		{
			_lock.Enter();
			try
			{
				var removedFiles = _cache.Keys.Except(files.Select(f => f.Path), _pathComparer).ToList();

				foreach (var file in removedFiles)
				{
					if (!_cache.TryRemove(file, out var entry))
						continue;

					foreach (var addon in entry.Addons)
						_addons.Remove(addon);
				}

				CacheEntry CreateCacheEntry(AddonPathInfo file, FileInfo? fileInfo)
				{
					if (!File.Exists(file.Path))
					{
						return new CacheEntry
						{
							LastWriteTime = DateTime.MinValue,
							FileSize = 0,
							Addons = [diagnosticFactory.CreateDiagnosticAddon(file, new AddonDiagnostic
								{
									IsFatal = true,
									Codes = AddonDiagnosticCode.MissingFile
								})]
						};
					}

					string fileContents;
					try
					{
						fileInfo ??= new FileInfo(file.Path);
						fileContents = File.ReadAllText(file.Path);
					}
					catch (Exception ex)
					{
						return new CacheEntry
						{
							LastWriteTime = DateTime.MinValue,
							FileSize = 0,
							Addons = [diagnosticFactory.CreateDiagnosticAddon(file, new AddonDiagnostic
								{
									IsFatal = true,
									Codes = AddonDiagnosticCode.FileAccessError,
									Exceptions = [ex]
								})]
						};
					}

					try
					{
						var addons = parser.Parse(fileContents, file);
						return new CacheEntry
						{
							LastWriteTime = fileInfo.LastWriteTime,
							FileSize = fileInfo.Length,
							Addons = [.. addons]
						};
					}
					catch (Exception ex)
					{
						return new CacheEntry
						{
							LastWriteTime = DateTime.MinValue,
							FileSize = 0,
							Addons = [diagnosticFactory.CreateDiagnosticAddon(file, new AddonDiagnostic
								{
									IsFatal = true,
									Codes = AddonDiagnosticCode.GeneralParsingError,
									Exceptions = [ex]
								})]
						};
					}
				}

				foreach (var file in files.DistinctBy(p => p.Path, _pathComparer))
				{
					_cache.AddOrUpdate(file.Path,
						path =>
						{
							CacheEntry entry = CreateCacheEntry(file, null);
							_addons.AddRange(entry.Addons);
							return entry;
						},
						(path, entry) =>
						{
							FileInfo fileInfo;
							try
							{
								fileInfo = new FileInfo(path);
								if (entry.LastWriteTime == fileInfo.LastWriteTime && entry.FileSize == fileInfo.Length)
								{
									return entry;
								}
								else
								{
									foreach (var addon in entry.Addons)
										_addons.Remove(addon);
								}
							}
							catch
							{
								return entry;
							}

							entry = CreateCacheEntry(file, fileInfo);
							_addons.AddRange(entry.Addons);
							return entry;
						});
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to load addons: {Error}", ex);
			}
			finally
			{
				_lock.Exit();
			}
		}
	}
}
