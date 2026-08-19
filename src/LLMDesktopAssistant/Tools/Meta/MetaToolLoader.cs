using System.Collections.Concurrent;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Services;

namespace LLMDesktopAssistant.Tools.Meta
{
	/// <summary>
	/// Loads meta tool files using <see cref="IMetaToolParser"/> and caches the results
	/// by file last write time.
	/// </summary>
	[ChatService(typeof(IMetaToolLoader))]
	public class MetaToolLoader(
		IEnumerable<IMetaToolEngine> engines,
		IMetaToolParser parser
	) : IMetaToolLoader
	{
		private class MetaToolCacheEntry
		{
			public required DateTime LastWriteTime { get; init; }
			public required MetaToolInfo MetaToolInfo { get; init; }
		}

		private readonly Dictionary<string, IMetaToolEngine> _enginesByExtension = engines
			.SelectMany(e => e.Descriptor.Extensions.Select(ext => (ext, e)))
			.ToDictionary(x => x.ext, x => x.e, StringComparer.OrdinalIgnoreCase);

		private readonly ConcurrentDictionary<string, MetaToolCacheEntry> _cache = [];

		/// <inheritdoc/>
		public IEnumerable<MetaToolInfo> Load(IEnumerable<MetaToolFileInfo> files)
		{
			foreach (var file in files)
			{
				var extension = Path.GetExtension(file.FileName);
				if (!_enginesByExtension.TryGetValue(extension, out var engine))
					continue; // Unknown extension — not a meta tool file.

				if (!File.Exists(file.FileName))
				{
					_cache.TryRemove(file.FileName, out _);
					yield return CreateDiagnosticInfo(file, engine, new MetaToolDiagnostic
					{
						IsFatal = true,
						Codes = MetaToolDiagnosticCode.MissingFile,
						Exception = null
					});
					continue;
				}

				yield return _cache.AddOrUpdate(file.FileName,
					_ => CreateCacheEntry(file, engine),
					(_, existingEntry) =>
					{
						FileInfo fileInfo;
						try
						{
							fileInfo = new FileInfo(file.FileName);
						}
						catch (Exception ex)
						{
							return new MetaToolCacheEntry
							{
								LastWriteTime = DateTime.MinValue,
								MetaToolInfo = CreateDiagnosticInfo(file, engine, new MetaToolDiagnostic
								{
									IsFatal = true,
									Codes = MetaToolDiagnosticCode.FileAccessError,
									Exception = ex
								})
							};
						}

						if (fileInfo.LastWriteTime == existingEntry.LastWriteTime)
							return existingEntry;

						return CreateCacheEntry(file, engine);
					}).MetaToolInfo;
			}
		}

		private MetaToolCacheEntry CreateCacheEntry(MetaToolFileInfo file, IMetaToolEngine engine)
		{
			FileInfo fileInfo;
			try
			{
				fileInfo = new FileInfo(file.FileName);
			}
			catch (Exception ex)
			{
				return new MetaToolCacheEntry
				{
					LastWriteTime = DateTime.MinValue,
					MetaToolInfo = CreateDiagnosticInfo(file, engine, new MetaToolDiagnostic
					{
						IsFatal = true,
						Codes = MetaToolDiagnosticCode.FileAccessError,
						Exception = ex
					})
				};
			}

			string contents;
			try
			{
				contents = File.ReadAllText(file.FileName);
			}
			catch (Exception ex)
			{
				return new MetaToolCacheEntry
				{
					LastWriteTime = DateTime.MinValue, // File access errors may be random, pass invalid time
					MetaToolInfo = CreateDiagnosticInfo(file, engine, new MetaToolDiagnostic
					{
						IsFatal = true,
						Codes = MetaToolDiagnosticCode.FileAccessError,
						Exception = ex
					})
				};
			}

			try
			{
				var info = parser.Parse(file.FileName, contents, file.Source, engine.Descriptor);
				return new MetaToolCacheEntry
				{
					LastWriteTime = fileInfo.LastWriteTime,
					MetaToolInfo = info
				};
			}
			catch (Exception ex)
			{
				return new MetaToolCacheEntry
				{
					LastWriteTime = fileInfo.LastWriteTime,
					MetaToolInfo = CreateDiagnosticInfo(file, engine, new MetaToolDiagnostic
					{
						IsFatal = true,
						Codes = MetaToolDiagnosticCode.GeneralParsingError,
						Exception = ex
					})
				};
			}
		}

		private static MetaToolInfo CreateDiagnosticInfo(MetaToolFileInfo file, IMetaToolEngine engine, MetaToolDiagnostic diagnostic) => new()
		{
			Name = Path.GetFileNameWithoutExtension(file.FileName),
			Source = file.Source,
			Path = file.FileName,
			ScriptLanguage = engine.Descriptor.Language,
			Diagnostic = diagnostic
		};
	}
}
