using System.Collections.Specialized;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Prompting.Parameterization;
using LLTSharp;
using LLTSharp.Locale;
using LLTSharp.Metadata;

namespace LLMDesktopAssistant.Prompting.Management
{
	[ChatService(typeof(IChatTemplateImporter))]
	public class ChatTemplateImporter : Disposable, IChatTemplateImporter
	{
		private readonly TemplateLibrary _library;
		private readonly IAppTemplateImporter _appTemplateImporter;
		private readonly Dictionary<string, IImportablePromptPartManager> _promptPartManagers;

		public TemplateLibrary Library => _library;

		public ChatTemplateImporter(IAppTemplateImporter appTemplateImporter,
			IEnumerable<IImportablePromptPartManager> promptPartManagers)
		{
			_library = new TemplateLibrary();
			_library.MetadataFactories.Add(new ParameterSchemaTemplateMetadataFactory());
			_library.SetLanguageFallbackScheme(new HierarchicalLanguageFallbackScheme(LanguageCode.Invariant));

			_appTemplateImporter = appTemplateImporter;
			_promptPartManagers = promptPartManagers.ToDictionary(t => t.TemplateType);

			foreach (var template in appTemplateImporter.BuiltInTemplates)
			{
				_library.Add(template);
				if (template.Metadata.TryGetAdditional<string>("type") is string type &&
					_promptPartManagers.TryGetValue(type, out var manager))
				{
					manager.ImportFromTemplate(template, PromptPartSource.BuiltInTemplate);
				}
			}

			_appTemplateImporter.UserTemplates.CollectionChanged += UserTemplates_CollectionChanged;
			UserTemplates_CollectionChanged(null, new NotifyCollectionChangedEventArgs(
				NotifyCollectionChangedAction.Add, _appTemplateImporter.UserTemplates));
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
			{
				_appTemplateImporter.UserTemplates.CollectionChanged -= UserTemplates_CollectionChanged;
			}
		}

		private void UserTemplates_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.OldItems != null)
				foreach (var item in e.OldItems)
				{
					if (item is not ITemplate template)
						continue;
					_library.Remove(template);

					if (template.Metadata.TryGetAdditional<string>("type") is string type &&
						_promptPartManagers.TryGetValue(type, out var manager))
					{
						manager.DropTemplate(template);
					}
				}

			if (e.NewItems != null)
				foreach (var item in e.NewItems)
				{
					if (item is not ITemplate template)
						continue;
					_library.Add(template);

					if (template.Metadata.TryGetAdditional<string>("type") is string type &&
						_promptPartManagers.TryGetValue(type, out var manager))
					{
						manager.ImportFromTemplate(template, PromptPartSource.UserTemplate);
					}
				}
		}
	}
}
