using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.Core.Settings;
using LLTSharp;
using LLTSharp.Metadata;

namespace LLMDesktopAssistant.Core.Prompting
{
	public static class PromptComposer
	{
		private static readonly TemplateLibrary _componentsLibrary = new();
		private static readonly TemplateLibrary _personasLibrary = new();

		static PromptComposer()
		{
			var assembly = typeof(PromptComposer).Assembly;

			var componentsFileStream = assembly.GetManifestResourceStream("LLMDesktopAssistant.Core.Prompts.components.llt")
				?? throw new FileNotFoundException("components.llt not found in embedded resources.");
			_componentsLibrary.ImportFromStream(componentsFileStream);

			var componentsConfig = SettingsManager.Get<PromptComponentsConfiguration>();

			foreach (var componentTemplate in _componentsLibrary)
			{
				var id = componentTemplate.Metadata.TryGet<TemplateIdentifierMetadata>();
				if (string.IsNullOrEmpty(id?.Identifier))
					continue;

			}

			var personasFileStream = assembly.GetManifestResourceStream("LLMDesktopAssistant.Core.Prompts.personas.llt")
				?? throw new FileNotFoundException("personas.llt not found in embedded resources.");
			_personasLibrary.ImportFromStream(personasFileStream);

			var personasConfig = SettingsManager.Get<PersonasConfiguration>();
		}

		public static string ComposeSystemPrompt(IEnumerable<Guid> componentIds, Guid? personaId)
		{
			return string.Empty;
		}
	}
}