using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;
using LLTSharp;
using LLTSharp.DataAccessors;
using LLTSharp.Locale;
using LLTSharp.Metadata;
using RCParsing;
using Serilog;

namespace LLMDesktopAssistant.Prompting
{
	public class PromptRegistry
	{
		public static TemplateLibrary SharedLibrary { get; } = new TemplateLibrary();

		public static ImmutableDictionary<Guid, PromptComponent> BuiltinComponents { get; }
		public static ImmutableDictionary<Guid, Persona> BuiltinPersonas { get; }
		public static ImmutableDictionary<Guid, Specialization> BuiltinSpecializations { get; }
		public static ImmutableDictionary<Guid, BehaviourSlider> BuiltinSliders { get; }

		static PromptRegistry()
		{
			var assembly = typeof(PromptRegistry).Assembly;

			var errors = new List<ParsingException>();

			foreach (var observedAssembly in ReflectionUtility.ObservedAssemblies)
				errors.AddRange(SharedLibrary.ImportFromAssembly(observedAssembly));

			if (errors.Count > 0)
				throw new AggregateException("Failed to import templates from assemblies.", errors);

			SharedLibrary.SetLanguageFallbackScheme(new HierarchicalLanguageFallbackScheme());

			var componentsBuilder = ImmutableDictionary.CreateBuilder<Guid, PromptComponent>();
			var personasBuilder = ImmutableDictionary.CreateBuilder<Guid, Persona>();
			var specializationsBuilder = ImmutableDictionary.CreateBuilder<Guid, Specialization>();
			var slidersBuilder = ImmutableDictionary.CreateBuilder<Guid, BehaviourSlider>();

			foreach (var template in SharedLibrary)
			{
				var id = template.Metadata.TryGet<TemplateIdentifierMetadata>()!.Identifier;
				var type = template.Metadata.TryGetAdditional<string>("type");

				// Log.Debug("Loading template: {Id}", id);

				if (type == "component" || type == "persona" || type == "specialization" || type == "slider")
				{
					if (template is not ITextTemplate textTemplate)
						throw new InvalidDataException($"Invalid template: {id} is not a text template.");

					var title = template.Metadata.TryGetAdditional<string>("title");
					var guidStr = template.Metadata.TryGetAdditional<string>("guid")
						?? throw new InvalidDataException($"Invalid template: {id} missing 'guid' metadata.");
					var guid = Guid.Parse(guidStr);
					var category = template.Metadata.TryGetAdditional<string>("category") ?? string.Empty;
					

					switch (type)
					{
						case "component":
							componentsBuilder.Add(guid, new PromptComponent
							{
								Id = guid,
								Name = title ?? LocalizationManager.LocalizeStatic("promptcomponent-" + id),
								Category = category,
								Template = new SerializableTextTemplate(textTemplate)
							});
							break;

						case "persona":
							personasBuilder.Add(guid, new Persona
							{
								Id = guid,
								Name = title ?? LocalizationManager.LocalizeStatic("persona-" + id),
								Template = new SerializableTextTemplate(textTemplate)
							});
							break;

						case "specialization":
							specializationsBuilder.Add(guid, new Specialization
							{
								Id = guid,
								Name = title ?? LocalizationManager.LocalizeStatic("specialization-" + id),
								Category = category,
								Template = new SerializableTextTemplate(textTemplate)
							});
							break;

						case "slider":
							var hintsRaw = template.Metadata.TryGetAdditional<object?[]>("hints") ?? [];
							var sliderMin = template.Metadata.TryGetAdditional<int>("slider_min");
							var sliderMax = template.Metadata.TryGetAdditional<int>("slider_max");
							var sliderDefault = template.Metadata.TryGetAdditional<int>("slider_default");

							var hints = ImmutableDictionary.CreateBuilder<int, string>();
							var hintsLength = sliderMax - sliderMin + 1;

							if (hintsLength != hintsRaw.Length)
								throw new InvalidDataException($"Slider template '{id}' has invalid hints: " +
									$"length of hint array must be equal to length of range, but currently hints:{hintsRaw.Length} != range:{hintsLength}");

							if (hintsRaw != null)
							{
								for (int i = 0; i < hintsLength; i++)
								{
									hints[i + sliderMin] = hintsRaw[i]?.ToString() ?? string.Empty;
								}
							}

							slidersBuilder.Add(guid, new BehaviourSlider
							{
								Id = guid,
								Name = title ?? LocalizationManager.LocalizeStatic("behaviourslider-" + id),
								MinimumValue = sliderMin,
								MaximumValue = sliderMax,
								DefaultValue = sliderDefault,
								Titles = hints.ToImmutable(),
								Template = new SerializableTextTemplate(textTemplate)
							});
							break;
					}
				}
			}

			BuiltinComponents = componentsBuilder.ToImmutable();
			BuiltinPersonas = personasBuilder.ToImmutable();
			BuiltinSpecializations = specializationsBuilder.ToImmutable();
			BuiltinSliders = slidersBuilder.ToImmutable();
		}

		public static PromptComponent? GetComponent(Guid id)
		{
			var componentsConfig = SettingsManager.Get<PromptComponentsConfiguration>();
			foreach (var component1 in componentsConfig.Components)
				if (component1.Id == id)
					return component1;

			if (BuiltinComponents.TryGetValue(id, out var component2))
				return component2;

			return null;
		}

		public static Persona? GetPersona(Guid id)
		{
			var personasConfig = SettingsManager.Get<PersonasConfiguration>();
			foreach (var persona1 in personasConfig.Personas)
				if (persona1.Id == id)
					return persona1;

			if (BuiltinPersonas.TryGetValue(id, out var persona2))
				return persona2;

			return null;
		}

		public static Specialization? GetSpecialization(Guid id)
		{
			var specializationsConfig = SettingsManager.Get<SpecializationsConfiguration>();
			foreach (var specialization1 in specializationsConfig.Specializations)
				if (specialization1.Id == id)
					return specialization1;

			if (BuiltinSpecializations.TryGetValue(id, out var specialization2))
				return specialization2;

			return null;
		}

		public static BehaviourSlider? GetSlider(Guid id)
		{
			if (BuiltinSliders.TryGetValue(id, out var slider))
				return slider;

			return null;
		}
	}
}
