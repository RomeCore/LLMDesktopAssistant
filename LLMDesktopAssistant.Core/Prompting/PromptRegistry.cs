using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.Core.LLM.Domain;
using LLMDesktopAssistant.Core.LLM.Services;
using LLMDesktopAssistant.Core.Settings;
using LLTSharp;
using LLTSharp.Metadata;

namespace LLMDesktopAssistant.Core.Prompting
{
	public class PromptRegistry
	{
		public static TemplateLibrary SharedLibrary { get; } = new TemplateLibrary();

		public static ImmutableDictionary<Guid, PromptComponent> BuiltinComponents { get; }
		public static ImmutableDictionary<Guid, Persona> BuiltinPersonas { get; }

		static PromptRegistry()
		{
			var assembly = typeof(PromptRegistry).Assembly;

			SharedLibrary.ImportFromAssembly(assembly);

			var componentsFileStream = assembly.GetManifestResourceStream("LLMDesktopAssistant.Core.Prompts.Resources.components.llt")
				?? throw new FileNotFoundException("components.llt not found in embedded resources.");
			var componentsLibrary = new TemplateLibrary();
			componentsLibrary.ImportFromStream(componentsFileStream);
			var componentsBuilder = ImmutableDictionary.CreateBuilder<Guid, PromptComponent>();

			foreach (var componentTemplate in componentsLibrary)
			{
				var id = componentTemplate.Metadata.TryGet<TemplateIdentifierMetadata>()!.Identifier;

				var guidStr = componentTemplate.Metadata.TryGetAdditional<string>("guid")
					?? throw new InvalidDataException($"Invalid component template: {id} missing 'guid' metadata.");
				var guid = Guid.Parse(guidStr);

				componentsBuilder.Add(guid, new PromptComponent
				{
					Id = guid,
					Name = id,
					Text = componentTemplate.Render().ToString() ?? throw new InvalidOperationException($"Failed to render component template: {id}")
				});
			}

			BuiltinComponents = componentsBuilder.ToImmutable();

			var personasFileStream = assembly.GetManifestResourceStream("LLMDesktopAssistant.Core.Prompts.Resources.personas.llt")
				?? throw new FileNotFoundException("personas.llt not found in embedded resources.");
			var personasLibrary = new TemplateLibrary();
			personasLibrary.ImportFromStream(personasFileStream);
			var personasBuilder = ImmutableDictionary.CreateBuilder<Guid, Persona>();

			foreach (var personaTemplate in personasLibrary)
			{
				var id = personaTemplate.Metadata.TryGet<TemplateIdentifierMetadata>()!.Identifier;

				var guidStr = personaTemplate.Metadata.TryGetAdditional<string>("guid")
					?? throw new InvalidDataException($"Invalid persona template: {id} missing 'guid' metadata.");
				var guid = Guid.Parse(guidStr);

				personasBuilder.Add(guid, new Persona
				{
					Id = guid,
					Name = id,
					Text = personaTemplate.Render().ToString() ?? throw new InvalidOperationException($"Failed to render persona template: {id}")
				});
			}

			BuiltinPersonas = personasBuilder.ToImmutable();
		}

		public static PromptComponent GetComponent(Guid id)
		{
			var componentsConfig = SettingsManager.Get<PromptComponentsConfiguration>();
			foreach (var component1 in componentsConfig.Components)
				if (component1.Id == id)
					return component1;

			if (BuiltinComponents.TryGetValue(id, out var component2))
				return component2;

			throw new ArgumentException($"Component with ID {id} not found.");
		}

		public static Persona GetPersona(Guid id)
		{
			var personasConfig = SettingsManager.Get<PersonasConfiguration>();
			foreach (var persona1 in personasConfig.Personas)
				if (persona1.Id == id)
					return persona1;

			if (BuiltinPersonas.TryGetValue(id, out var persona2))
				return persona2;

			throw new ArgumentException($"Persona with ID {id} not found.");
		}
	}
}