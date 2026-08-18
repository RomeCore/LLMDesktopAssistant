using System.Collections.Concurrent;
using LLMDesktopAssistant.Services;

namespace LLMDesktopAssistant.Agents.SubAgents
{
	[Service(typeof(ISubAgentLoader))]
	public class SubAgentLoader(
		ISubAgentParser parser
	) : ISubAgentLoader
	{
		private class SubAgentCacheEntry
		{
			public required DateTime LastWriteTime { get; init; }
			public required SubAgentInfo SubAgentInfo { get; init; }
		}

		private readonly ConcurrentDictionary<string, SubAgentCacheEntry> _cache = [];

		public IEnumerable<SubAgentInfo> Load(IEnumerable<SubAgentFileInfo> files)
		{
			foreach (var file in files)
			{
				var fallbackSubAgentName = Path.GetFileNameWithoutExtension(file.FileName);
				var nameUnknownCode = string.IsNullOrEmpty(fallbackSubAgentName)
					? SubAgentDiagnosticCode.MissingName
					: SubAgentDiagnosticCode.None;
				fallbackSubAgentName = string.IsNullOrEmpty(fallbackSubAgentName) ? "unknown" : fallbackSubAgentName;

				if (!File.Exists(file.FileName))
				{
					_cache.TryRemove(file.FileName, out _);
					yield return CreateDiagnosticSubAgent(fallbackSubAgentName, file.FileName, new SubAgentDiagnostic
					{
						IsFatal = true,
						Codes = SubAgentDiagnosticCode.MissingFile | nameUnknownCode,
						Exception = null
					});
					continue;
				}

				SubAgentCacheEntry CreateCacheEntry(FileInfo fileInfo, SubAgentFileInfo file)
				{
					string contents;
					try
					{
						contents = File.ReadAllText(file.FileName);
					}
					catch (Exception ex)
					{
						var subAgent = CreateDiagnosticSubAgent(fallbackSubAgentName, file.FileName, new SubAgentDiagnostic
						{
							IsFatal = true,
							Codes = SubAgentDiagnosticCode.FileAccessError | nameUnknownCode,
							Exception = ex
						});
						return new SubAgentCacheEntry
						{
							LastWriteTime = DateTime.MinValue, // File access errors may be random, pass invalid time
							SubAgentInfo = subAgent
						};
					}

					try
					{
						var subAgent = parser.Parse(file.FileName, contents!, file.Source);
						return new SubAgentCacheEntry
						{
							LastWriteTime = fileInfo.LastWriteTime,
							SubAgentInfo = subAgent
						};
					}
					catch (Exception ex)
					{
						var subAgent = CreateDiagnosticSubAgent(fallbackSubAgentName, file.FileName, new SubAgentDiagnostic
						{
							IsFatal = true,
							Codes = SubAgentDiagnosticCode.GeneralParsingError | nameUnknownCode,
							Exception = ex
						});
						return new SubAgentCacheEntry
						{
							LastWriteTime = fileInfo.LastWriteTime,
							SubAgentInfo = subAgent
						};
					}
				}

				yield return _cache.AddOrUpdate(file.FileName,
					_ =>
					{
						FileInfo fileInfo;
						try
						{
							fileInfo = new FileInfo(file.FileName);
						}
						catch (Exception ex)
						{
							var subAgent = CreateDiagnosticSubAgent(fallbackSubAgentName, file.FileName, new SubAgentDiagnostic
							{
								IsFatal = true,
								Codes = SubAgentDiagnosticCode.FileAccessError | nameUnknownCode,
								Exception = ex
							});
							return new SubAgentCacheEntry
							{
								LastWriteTime = DateTime.MinValue,
								SubAgentInfo = subAgent
							};
						}
						return CreateCacheEntry(fileInfo, file);
					},
					(_, existingEntry) =>
					{
						FileInfo fileInfo;
						try
						{
							fileInfo = new FileInfo(file.FileName);
						}
						catch (Exception ex)
						{
							var subAgent = CreateDiagnosticSubAgent(fallbackSubAgentName, file.FileName, new SubAgentDiagnostic
							{
								IsFatal = true,
								Codes = SubAgentDiagnosticCode.FileAccessError | nameUnknownCode,
								Exception = ex
							});
							return new SubAgentCacheEntry
							{
								LastWriteTime = DateTime.MinValue,
								SubAgentInfo = subAgent
							};
						}
						if (fileInfo.LastWriteTime == existingEntry.LastWriteTime)
						{
							// File has not changed since last load, return cached sub-agent info
							return existingEntry;
						}
						return CreateCacheEntry(fileInfo, file);
					}).SubAgentInfo;
			}
		}

		private static SubAgentInfo CreateDiagnosticSubAgent(string name, string file, SubAgentDiagnostic diagnostic)
		{
			return new SubAgentInfo
			{
				Name = name,
				Description = string.Empty,
				Source = SubAgentSource.Unknown,
				BodyGetter = new(() => string.Empty),
				Diagnostic = diagnostic,
				Path = file,
				HomeDirectory = Path.GetDirectoryName(file)
			};
		}
	}
}
