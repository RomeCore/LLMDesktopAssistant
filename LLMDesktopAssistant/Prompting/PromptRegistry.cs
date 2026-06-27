using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;
using LLTSharp;
using LLTSharp.DataAccessors;
using LLTSharp.Locale;
using LLTSharp.Metadata;
using LLTSharp.Metadata.Types;
using RCParsing;
using Serilog;

namespace LLMDesktopAssistant.Prompting
{
	[Service(typeof(IPromptRegistry))]
	public class PromptRegistry : IPromptRegistry
	{
		public TemplateLibrary SharedLibrary { get; } = new TemplateLibrary();

		/// <summary>
		/// Gets all built-in prompt components.
		/// </summary>
		public ImmutableDictionary<(Guid, LanguageCode), PromptComponent> AllBuiltinComponents { get; }

		/// <summary>
		/// Gets all built-in personas.
		/// </summary>
		public ImmutableDictionary<(Guid, LanguageCode), Persona> AllBuiltinPersonas { get; }

		/// <summary>
		/// Gets all built-in specializations.
		/// </summary>
		public ImmutableDictionary<(Guid, LanguageCode), Specialization> AllBuiltinSpecializations { get; }

		/// <summary>
		/// Gets all built-in behaviour sliders.
		/// </summary>
		public ImmutableDictionary<(Guid, LanguageCode), BehaviourSlider> AllBuiltinSliders { get; }

		/// <summary>
		/// Gets target built-in prompt components mapped by their unique identifier.
		/// </summary>
		public ImmutableDictionary<Guid, PromptComponent> BuiltinComponents { get; private set; } = null!;

		/// <summary>
		/// Gets target built-in personas mapped by their unique identifier.
		/// </summary>
		public ImmutableDictionary<Guid, Persona> BuiltinPersonas { get; private set; } = null!;

		/// <summary>
		/// Gets target built-in specializations mapped by their unique identifier.
		/// </summary>
		public ImmutableDictionary<Guid, Specialization> BuiltinSpecializations { get; private set; } = null!;

		/// <summary>
		/// Gets target built-in behaviour sliders mapped by their unique identifier.
		/// </summary>
		public ImmutableDictionary<Guid, BehaviourSlider> BuiltinSliders { get; private set; } = null!;

		public PromptRegistry()
		{
			var assembly = typeof(PromptRegistry).Assembly;

			var errors = new List<ParsingException>();

			foreach (var observedAssembly in ReflectionUtility.ObservedAssemblies)
				errors.AddRange(SharedLibrary.ImportFromAssembly(observedAssembly));

			if (errors.Count > 0)
				throw new AggregateException("Failed to import templates from assemblies.", errors);

			errors.AddRange(SharedLibrary.ImportFromFolder(Directories.Templates, recursive: true));

			if (errors.Count > 0)
				Log.Error("Errors occured when parsing additional templates: "
					+ string.Join(", ", Enumerable.Range(0, errors.Count).Select(i => $"{{Error{i}}}")),
					errors.Cast<object>().ToArray());

			SharedLibrary.SetLanguageFallbackScheme(new HierarchicalLanguageFallbackScheme());

			var allComponentsBuilder = ImmutableDictionary.CreateBuilder<(Guid, LanguageCode), PromptComponent>();
			var allPersonasBuilder = ImmutableDictionary.CreateBuilder<(Guid, LanguageCode), Persona>();
			var allSpecializationsBuilder = ImmutableDictionary.CreateBuilder<(Guid, LanguageCode), Specialization>();
			var allSlidersBuilder = ImmutableDictionary.CreateBuilder<(Guid, LanguageCode), BehaviourSlider>();

			foreach (var template in SharedLibrary)
			{
				var id = template.Metadata.TryGet<TemplateIdentifierMetadata>()!.Identifier;
				var type = template.Metadata.TryGetAdditional<string>("type");

				// Log.Debug("Loading template: {Id}", id);

				if (type == "component" || type == "persona" || type == "specialization" || type == "slider")
				{
					if (template is not ITextTemplate textTemplate)
						throw new InvalidDataException($"Invalid template: {id} is not a text template.");

					var guidStr = template.Metadata.TryGetAdditional<string>("guid")
						?? throw new InvalidDataException($"Invalid template: {id} missing 'guid' metadata.");
					var lang = template.Metadata.TryGet<LanguageMetadata>()
						?? throw new InvalidDataException($"Invalid template: {id} missing 'lang' metadata.");
					var title = template.Metadata.TryGetAdditional<string>("title");
					var guid = Guid.Parse(guidStr);
					var category = template.Metadata.TryGetAdditional<string>("category") ?? string.Empty;
					var localizedForRaw = template.Metadata.TryGetAdditional<string>("localized_for");
					LanguageCode? localizedFor = localizedForRaw != null ? new LanguageCode(localizedForRaw) : null;

					switch (type)
					{
						case "component":
							if (allComponentsBuilder.ContainsKey((guid, lang.LanguageCode)))
								Log.Warning("PromptRegistry: Duplicate component found: {Id}, {Lang}", guid, lang.LanguageCode);
							allComponentsBuilder[(guid, lang.LanguageCode)] = new PromptComponent
							{
								Id = guid,
								Name = title ?? LocalizationManager.LocalizeStatic("promptcomponent-" + id),
								Category = category,
								LocalizedFor = localizedFor,
								Template = new SerializableTextTemplate(textTemplate)
							};
							break;

						case "persona":
							if (allPersonasBuilder.ContainsKey((guid, lang.LanguageCode)))
								Log.Warning("PromptRegistry: Duplicate persona found: {Id}, {Lang}", guid, lang.LanguageCode);
							allPersonasBuilder[(guid, lang.LanguageCode)] = new Persona
							{
								Id = guid,
								Name = title ?? LocalizationManager.LocalizeStatic("persona-" + id),
								Category = category,
								LocalizedFor = localizedFor,
								Template = new SerializableTextTemplate(textTemplate)
							};
							break;

						case "specialization":
							if (allSpecializationsBuilder.ContainsKey((guid, lang.LanguageCode)))
								Log.Warning("PromptRegistry: Duplicate specialization found: {Id}, {Lang}", guid, lang.LanguageCode);
							allSpecializationsBuilder[(guid, lang.LanguageCode)] = new Specialization
							{
								Id = guid,
								Name = title ?? LocalizationManager.LocalizeStatic("specialization-" + id),
								Category = category,
								LocalizedFor = localizedFor,
								Template = new SerializableTextTemplate(textTemplate)
							};
							break;

						case "slider":
							if (allSlidersBuilder.ContainsKey((guid, lang.LanguageCode)))
								Log.Warning("PromptRegistry: Duplicate slider found: {Id}, {Lang}", guid, lang.LanguageCode);

							var hintsRaw = template.Metadata.TryGetAdditional<object?[]>("hints") ?? [];
							var sliderMin = (int)template.Metadata.TryGetAdditional<double>("slider_min");
							var sliderMax = (int)template.Metadata.TryGetAdditional<double>("slider_max");
							var sliderDefault = (int)template.Metadata.TryGetAdditional<double>("slider_default");

							var hints = ImmutableDictionary.CreateBuilder<int, string>();
							var hintsLength = sliderMin != 0 && sliderMax != 0 ? sliderMax - sliderMin + 1 : hintsRaw.Length;

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

							allSlidersBuilder[(guid, lang.LanguageCode)] = new BehaviourSlider
							{
								Id = guid,
								Name = title ?? LocalizationManager.LocalizeStatic("behaviourslider-" + id),
								Category = category,
								LocalizedFor = localizedFor,
								Titles = hints.ToImmutable(),
								MinimumValue = sliderMin,
								MaximumValue = sliderMax,
								DefaultValue = sliderDefault,
								Template = new SerializableTextTemplate(textTemplate)
							};
							break;
					}
				}
			}

			// Ахахха бля, можно скобочки так делать оказывается
			foreach (var ((guid, lang), component) in allComponentsBuilder)
			{
				if (!component.LocalizedFor.HasValue) continue;

				if (!allComponentsBuilder.TryGetValue((guid, component.LocalizedFor.Value), out var componentToExtend))
				{
					Log.Error("PromptRegistry: Cannot find component to extend: {Id}, {Lang}", guid, component.LocalizedFor.Value);
					continue;
				}

				component.Template = componentToExtend.Template;
			}

			foreach (var ((guid, lang), persona) in allPersonasBuilder)
			{
				if (!persona.LocalizedFor.HasValue) continue;

				if (!allPersonasBuilder.TryGetValue((guid, persona.LocalizedFor.Value), out var personaToExtend))
				{
					Log.Error("PromptRegistry: Cannot find persona to extend: {Id}, {Lang}", guid, persona.LocalizedFor.Value);
					continue;
				}

				persona.Template = personaToExtend.Template;
			}

			foreach (var ((guid, lang), specialization) in allSpecializationsBuilder)
			{
				if (!specialization.LocalizedFor.HasValue) continue;

				if (!allSpecializationsBuilder.TryGetValue((guid, specialization.LocalizedFor.Value), out var specializationToExtend))
				{
					Log.Error("PromptRegistry: Cannot find specialization to extend: {Id}, {Lang}", guid, specialization.LocalizedFor.Value);
					continue;
				}

				specialization.Template = specializationToExtend.Template;
			}

			foreach (var ((guid, lang), slider) in allSlidersBuilder)
			{
				if (!slider.LocalizedFor.HasValue) continue;

				if (!allSlidersBuilder.TryGetValue((guid, slider.LocalizedFor.Value), out var sliderToExtend))
				{
					Log.Error("PromptRegistry: Cannot find behaviour slider to extend: {Id}, {Lang}", guid, slider.LocalizedFor.Value);
					continue;
				}

				slider.DefaultValue = sliderToExtend.DefaultValue;
				slider.MinimumValue = sliderToExtend.MinimumValue;
				slider.MaximumValue = sliderToExtend.MaximumValue;
				slider.Template = sliderToExtend.Template;
			}

			AllBuiltinComponents = allComponentsBuilder.ToImmutable();
			AllBuiltinPersonas = allPersonasBuilder.ToImmutable();
			AllBuiltinSpecializations = allSpecializationsBuilder.ToImmutable();
			AllBuiltinSliders = allSlidersBuilder.ToImmutable();

			RefreshTargetBuiltinPrompts();
		}

		private void RefreshTargetBuiltinPrompts()
		{
			var componentsBuilder = ImmutableDictionary.CreateBuilder<Guid, PromptComponent>();
			var personasBuilder = ImmutableDictionary.CreateBuilder<Guid, Persona>();
			var specializationsBuilder = ImmutableDictionary.CreateBuilder<Guid, Specialization>();
			var slidersBuilder = ImmutableDictionary.CreateBuilder<Guid, BehaviourSlider>();

			foreach (var ((guid, lang), component) in AllBuiltinComponents)
				if (ShouldAdd(componentsBuilder, guid, lang))
					componentsBuilder[guid] = component;

			foreach (var ((guid, lang), persona) in AllBuiltinPersonas)
				if (ShouldAdd(personasBuilder, guid, lang))
					personasBuilder[guid] = persona;

			foreach (var ((guid, lang), specialization) in AllBuiltinSpecializations)
				if (ShouldAdd(specializationsBuilder, guid, lang))
					specializationsBuilder[guid] = specialization;

			foreach (var ((guid, lang), slider) in AllBuiltinSliders)
				if (ShouldAdd(slidersBuilder, guid, lang))
					slidersBuilder[guid] = slider;

			BuiltinComponents = componentsBuilder.ToImmutable();
			BuiltinPersonas = personasBuilder.ToImmutable();
			BuiltinSpecializations = specializationsBuilder.ToImmutable();
			BuiltinSliders = slidersBuilder.ToImmutable();
		}

		private bool ShouldAdd<T>(ImmutableDictionary<Guid, T>.Builder builder,
			Guid guid, LanguageCode lang)
		{
			if (!builder.ContainsKey(guid))
				return true;

			if (lang == new LanguageCode(System.Globalization.CultureInfo.CurrentCulture))
				return true;

			return false;
		}

		public PromptComponent? GetComponent(Guid id)
		{
			var componentsConfig = SettingsManager.Get<PromptComponentsConfiguration>();
			foreach (var component1 in componentsConfig.Components)
				if (component1.Id == id)
					return component1;

			if (BuiltinComponents.TryGetValue(id, out var component2))
				return component2;

			return null;
		}

		public Persona? GetPersona(Guid id)
		{
			var personasConfig = SettingsManager.Get<PersonasConfiguration>();
			foreach (var persona1 in personasConfig.Personas)
				if (persona1.Id == id)
					return persona1;

			if (BuiltinPersonas.TryGetValue(id, out var persona2))
				return persona2;

			return null;
		}

		public Specialization? GetSpecialization(Guid id)
		{
			var specializationsConfig = SettingsManager.Get<SpecializationsConfiguration>();
			foreach (var specialization1 in specializationsConfig.Specializations)
				if (specialization1.Id == id)
					return specialization1;

			if (BuiltinSpecializations.TryGetValue(id, out var specialization2))
				return specialization2;

			return null;
		}

		public BehaviourSlider? GetSlider(Guid id)
		{
			if (BuiltinSliders.TryGetValue(id, out var slider))
				return slider;

			return null;
		}
	}
}
