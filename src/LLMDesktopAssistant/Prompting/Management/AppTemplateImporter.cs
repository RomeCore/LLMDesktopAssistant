using System.Collections.Concurrent;
using LLMDesktopAssistant.Prompting.Parameterization;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Utils;
using LLTSharp;
using LLTSharp.Locale;
using RCParsing;

namespace LLMDesktopAssistant.Prompting.Management
{
	[Service(typeof(IAppTemplateImporter))]
	public class AppTemplateImporter : IAppTemplateImporter
	{
		private readonly TemplateLibrary _library;

		private readonly LLTParser _parser;
		private readonly ImmutableList<ITemplate> _builtInTemplates;
		private readonly RangeObservableCollection<ITemplate> _userTemplates;
		private readonly RangeObservableCollection<(string, Exception)> _importingErrors;

		public TemplateLibrary Library => _library;
		public IEnumerable<ITemplate> BuiltInTemplates => _builtInTemplates;
		public ReadOnlyObservableCollection<ITemplate> UserTemplates { get; }
		public ReadOnlyObservableCollection<(string, Exception)> ImportingErrors { get; }

		public AppTemplateImporter()
		{
			_library = new TemplateLibrary();
			_library.MetadataFactories.Add(new ParameterSchemaTemplateMetadataFactory());
			_library.SetLanguageFallbackScheme(new HierarchicalLanguageFallbackScheme(LanguageCode.Invariant));

			_parser = new LLTParser();
			_userTemplates = [];
			_importingErrors = [];

			UserTemplates = new ReadOnlyObservableCollection<ITemplate>(_userTemplates);
			ImportingErrors = new ReadOnlyObservableCollection<(string, Exception)>(_importingErrors);

			var embeddedParsingErrors = new List<ParsingException>();
			foreach (var asm in ReflectionUtility.ObservedAssemblies)
				embeddedParsingErrors.AddRange(_library.ImportFromAssembly(asm));
			if (embeddedParsingErrors.Count > 0)
				throw new AggregateException("Failed to parse built-in templates.", embeddedParsingErrors);

			_builtInTemplates = [.. _library];

			UpdateUserTemplates();

			var fsw = new FileSystemWatcher(Directories.Templates, "*.llt")
			{
				NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName | NotifyFilters.DirectoryName,
				EnableRaisingEvents = true
			};

			fsw.Changed += (s, e) => UpdateUserTemplates();
			fsw.Created += (s, e) => UpdateUserTemplates();
			fsw.Deleted += (s, e) => UpdateUserTemplates();
			fsw.Renamed += (s, e) => UpdateUserTemplates();
		}

		private class TemplateCacheEntry
		{
			public required DateTime LastWriteTime { get; init; }
			public required long FileSize { get; init; }
			public required ImmutableList<ITemplate> Templates { get; init; }
			public required ImmutableList<Exception> ImportingErrors { get; init; }
		}

		private readonly ConcurrentDictionary<string, TemplateCacheEntry> _templateCache = [];
		private int _userUpdating = 0;

		private void UpdateUserTemplates()
		{
			if (Interlocked.CompareExchange(ref _userUpdating, 1, 0) != 0)
				return;

			var files = Directory.GetFiles(Directories.Templates, "*.llt", SearchOption.AllDirectories);
			var removedFiles = _templateCache.Keys.Except(files).ToList();

			foreach (var file in removedFiles)
			{
				if (!_templateCache.TryRemove(file, out var entry))
					continue;

				if (entry.Templates.Count > 0)
					_library.RemoveRange(entry.Templates);
				foreach (var template in entry.Templates)
					_userTemplates.Remove(template);
				foreach (var error in entry.ImportingErrors)
					_importingErrors.Remove((file, error));
			}

			foreach (var file in files)
			{
				_templateCache.AddOrUpdate(file,
					file =>
					{
						try
						{
							var fileInfo = new FileInfo(file);
							var fileContents = File.ReadAllText(file);

							var templates = _parser.Parse(fileContents, _library.MetadataFactories);
							var entry = new TemplateCacheEntry
							{
								LastWriteTime = fileInfo.LastWriteTime,
								FileSize = fileInfo.Length,
								Templates = [.. templates],
								ImportingErrors = []
							};

							_library.AddRange(entry.Templates);
							_userTemplates.AddRange(entry.Templates);
							foreach (var ex in entry.ImportingErrors)
								_importingErrors.Add((file, ex));

							return entry;
						}
						catch (Exception ex)
						{
							_importingErrors.Add((file, ex));
							return new TemplateCacheEntry
							{
								LastWriteTime = DateTime.MinValue,
								FileSize = 0,
								Templates = [],
								ImportingErrors = [ex]
							};
						}
					},
					(file, existing) =>
					{
						FileInfo fileInfo;
						try
						{
							fileInfo = new FileInfo(file);
						}
						catch
						{
							return existing;
						}

						if (existing.LastWriteTime == fileInfo.LastWriteTime && existing.FileSize == fileInfo.Length)
						{
							return existing;
						}
						else
						{
							if (existing.Templates.Count > 0)
								_library.RemoveRange(existing.Templates);
							foreach (var template in existing.Templates)
								_userTemplates.Remove(template);
							foreach (var error in existing.ImportingErrors)
								_importingErrors.Remove((file, error));
						}

						try
						{
							var fileContents = File.ReadAllText(file);

							var templates = _parser.Parse(fileContents, _library.MetadataFactories);
							var entry = new TemplateCacheEntry
							{
								LastWriteTime = fileInfo.LastWriteTime,
								FileSize = fileInfo.Length,
								Templates = [.. templates],
								ImportingErrors = []
							};

							_library.AddRange(entry.Templates);
							_userTemplates.AddRange(entry.Templates);
							foreach (var ex in entry.ImportingErrors)
								_importingErrors.Add((file, ex));

							return entry;
						}
						catch (Exception ex)
						{
							_importingErrors.Add((file, ex));
							return new TemplateCacheEntry
							{
								LastWriteTime = DateTime.MinValue,
								FileSize = 0,
								Templates = [],
								ImportingErrors = [ex]
							};
						}

					});
			}

			_userUpdating = 0;
		}
	}
}
