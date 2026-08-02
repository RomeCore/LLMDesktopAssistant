using System.Collections.Concurrent;
using LLMDesktopAssistant.Services;
using RCLargeLanguageModels;

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

		public IEnumerable<SkillInfo> Load(IEnumerable<string> files)
		{
			foreach (var file in files)
			{
				var fallbackSkillName = Path.GetFileName(Path.GetDirectoryName(file));
				var nameUnknownCode = string.IsNullOrEmpty(fallbackSkillName)
					? SkillDiagnosticCode.MissingName
					: SkillDiagnosticCode.None;
				fallbackSkillName = string.IsNullOrEmpty(fallbackSkillName) ? "unknown" : fallbackSkillName;

				if (!File.Exists(file))
				{
					_cache.TryRemove(file, out _);
					yield return CreateDiagnosticSkill(fallbackSkillName, file, new SkillDiagnostic
					{
						IsFatal = true,
						Codes = SkillDiagnosticCode.MissingFile | nameUnknownCode,
						Exception = null
					});
					continue;
				}

				SkillCacheEntry CreateCacheEntry(FileInfo fileInfo, string file)
				{
					string contents;
					try
					{
						contents = File.ReadAllText(file);
					}
					catch (Exception ex)
					{
						var skill = CreateDiagnosticSkill(fallbackSkillName, file, new SkillDiagnostic
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
						var skill = parser.Parse(file, contents!);
						return new SkillCacheEntry
						{
							LastWriteTime = fileInfo.LastWriteTime,
							SkillInfo = skill
						};
					}
					catch (Exception ex)
					{
						var skill = CreateDiagnosticSkill(fallbackSkillName, file, new SkillDiagnostic
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

				yield return _cache.AddOrUpdate(file,
					file =>
					{
						FileInfo fileInfo;
						try
						{
							fileInfo = new FileInfo(file);
						}
						catch (Exception ex)
						{
							var skill = CreateDiagnosticSkill(fallbackSkillName, file, new SkillDiagnostic
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
					(file, existingEntry) =>
					{
						FileInfo fileInfo;
						try
						{
							fileInfo = new FileInfo(file);
						}
						catch (Exception ex)
						{
							var skill = CreateDiagnosticSkill(fallbackSkillName, file, new SkillDiagnostic
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
				BodyGetter = new(() => string.Empty),
				Diagnostic = diagnostic,
				Path = file,
				HomeDirectory = Path.GetDirectoryName(file)
			};
		}
	}
}
