using System.Collections.Concurrent;
using LLMDesktopAssistant.Services;

namespace LLMDesktopAssistant.Prompting.Skills
{
	[Service(typeof(ISkillLoader))]
	public class SkillLoader(
		ISkillParser parser
	) : ISkillLoader
	{
		private class SkillCacheEntry
		{
			public required DateTime LastWriteTime { get; init; }
			public required SkillInfo SkillInfo { get; init; }
		}

		private readonly ConcurrentDictionary<string, SkillCacheEntry> _cache = [];

		public IEnumerable<SkillInfo> Load(IEnumerable<SkillFileInfo> files)
		{
			foreach (var file in files)
			{
				var fallbackSkillName = Path.GetFileName(Path.GetDirectoryName(file.FileName));
				var nameUnknownCode = string.IsNullOrEmpty(fallbackSkillName)
					? SkillDiagnosticCode.MissingName
					: SkillDiagnosticCode.None;
				fallbackSkillName = string.IsNullOrEmpty(fallbackSkillName) ? "unknown" : fallbackSkillName;

				if (!File.Exists(file.FileName))
				{
					_cache.TryRemove(file.FileName, out _);
					yield return CreateDiagnosticSkill(fallbackSkillName, file.FileName, new SkillDiagnostic
					{
						IsFatal = true,
						Codes = SkillDiagnosticCode.MissingFile | nameUnknownCode,
						Exception = null
					});
					continue;
				}

				SkillCacheEntry CreateCacheEntry(FileInfo fileInfo, SkillFileInfo file)
				{
					string contents;
					try
					{
						contents = File.ReadAllText(file.FileName);
					}
					catch (Exception ex)
					{
						var skill = CreateDiagnosticSkill(fallbackSkillName, file.FileName, new SkillDiagnostic
						{
							IsFatal = true,
							Codes = SkillDiagnosticCode.FileAccessError | nameUnknownCode,
							Exception = ex
						});
						return new SkillCacheEntry
						{
							LastWriteTime = DateTime.MinValue, // File access errors may be random, pass invalid time
							SkillInfo = skill
						};
					}

					try
					{
						var skill = parser.Parse(file.FileName, contents!, file.Source);
						return new SkillCacheEntry
						{
							LastWriteTime = fileInfo.LastWriteTime,
							SkillInfo = skill
						};
					}
					catch (Exception ex)
					{
						var skill = CreateDiagnosticSkill(fallbackSkillName, file.FileName, new SkillDiagnostic
						{
							IsFatal = true,
							Codes = SkillDiagnosticCode.GeneralParsingError | nameUnknownCode,
							Exception = ex
						});
						return new SkillCacheEntry
						{
							LastWriteTime = fileInfo.LastWriteTime,
							SkillInfo = skill
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
							var skill = CreateDiagnosticSkill(fallbackSkillName, file.FileName, new SkillDiagnostic
							{
								IsFatal = true,
								Codes = SkillDiagnosticCode.FileAccessError | nameUnknownCode,
								Exception = ex
							});
							return new SkillCacheEntry
							{
								LastWriteTime = DateTime.MinValue,
								SkillInfo = skill
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
							var skill = CreateDiagnosticSkill(fallbackSkillName, file.FileName, new SkillDiagnostic
							{
								IsFatal = true,
								Codes = SkillDiagnosticCode.FileAccessError | nameUnknownCode,
								Exception = ex
							});
							return new SkillCacheEntry
							{
								LastWriteTime = DateTime.MinValue,
								SkillInfo = skill
							};
						}
						if (fileInfo.LastWriteTime == existingEntry.LastWriteTime)
						{
							// File has not changed since last load, return cached skill info
							return existingEntry;
						}
						return CreateCacheEntry(fileInfo, file);
					}).SkillInfo;
			}
		}

		private static SkillInfo CreateDiagnosticSkill(string name, string file, SkillDiagnostic diagnostic)
		{
			return new SkillInfo
			{
				Name = name,
				Description = string.Empty,
				Source = SkillSource.Unknown,
				BodyGetter = new(() => string.Empty),
				Diagnostic = diagnostic,
				Path = file,
				HomeDirectory = Path.GetDirectoryName(file)
			};
		}
	}
}
