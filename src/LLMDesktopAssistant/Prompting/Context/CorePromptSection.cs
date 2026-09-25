using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Agents.Settings;
using LLMDesktopAssistant.Agents.SubAgents;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.LLM.Services.Prompting;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Prompting.ContextExpanders;
using LLMDesktopAssistant.Prompting.Management;
using LLMDesktopAssistant.Prompting.Plugins;
using LLMDesktopAssistant.Prompting.Skills;
using LLMDesktopAssistant.StructuredValues.Converters;
using LLTSharp;

namespace LLMDesktopAssistant.Prompting.Context
{
	/// <summary>
	/// The state of the core prompt section.
	/// </summary>
	public class CorePromptSectionState : PromptSectionStateBase
	{
		private string _text = string.Empty;
		/// <summary>
		/// The rendered text of the core prompt.
		/// </summary>
		public string Text
		{
			get => _text;
			set => SetProperty(ref _text, value);
		}
	}

	/// <summary>
	/// The delta of the core prompt section (stub).
	/// </summary>
	public class CorePromptSectionDelta : PromptSectionDeltaBase
	{
	}

	/// <summary>
	/// Captures the core prompt state by rendering the section template
	/// (the old system_prompt.llt monolith as its own render).
	/// </summary>
	[ChatService(typeof(IPromptSectionStateProvider<CorePromptSectionState>))]
	public class CorePromptStateProvider(
		ITemplateLibraryAccessor templates,
		IChatSettingsService chatSettings,
		IPromptSlotElementManager slotElementManager,
		IPromptComponentManager componentManager,
		IAddonSetCollector<SkillInfo> skillsetBuilder,
		IAddonSetCollector<SubAgentInfo> subAgentsetCollector,
		IEnumerable<IPromptSystemContextExpander> promptSystemContextExpanders,
		IEnumerable<IPromptTemplatePlugin> promptTemplatePlugins
		) : IPromptSectionStateProvider<CorePromptSectionState>
	{
		/// <inheritdoc/>
		public CorePromptSectionState CaptureState(ChatAgentDescriptor agent)
		{
			var template = templates.GetTextTemplate("core_prompt");
			var functions = new TemplateFunctionSet(promptTemplatePlugins.SelectMany(p => p.GetTemplateFunctions()));
			var promptSettings = agent.Prompts;

			var generalContext = new Dictionary<string, object?>();
			foreach (var expander in promptSystemContextExpanders)
				expander.ExpandPromptContext(generalContext);
			var partsContext = generalContext.ToDictionary();

			var effectiveSystemPrompt = promptSettings.GetEffectiveSystemPrompt(chatSettings.Settings);
			var effectiveComponents = promptSettings.GetEffectivePromptComponents(chatSettings.Settings);
			var effectivePersona = promptSettings.GetEffectivePersona(chatSettings.Settings);
			var effectiveSpecialization = promptSettings.GetEffectiveSpecialization(chatSettings.Settings);
			var effectiveChatMemoryOptions = chatSettings.Settings.Memory.GetEffectiveMemoryOptions();

			string? RenderPromptPart<K, V>(IPromptPartManager<K, V> manager, PromptPartSelection selection, K key)
				where K : notnull
				where V : PromptPartBase
			{
				var part = manager.TryGet(key);
				if (part is null)
					return null;
				if (part.ParameterSchema is not null)
				{
					selection.Parameters = part.ParameterSchema.Root.CreateOrFixValue(selection.Parameters, []);
					partsContext["params"] = LLTStructuredConverter.ToTemplateDataAccessor(selection.Parameters);
				}
				var result = part.EffectiveTemplate.Render(partsContext, functions).ToString();
				partsContext.Remove("params");
				return result;
			}

			generalContext["prompt"] =
				effectiveSystemPrompt.UseCustomSystemPrompt ? effectiveSystemPrompt.CustomSystemPrompt :
				RenderPromptPart(slotElementManager, effectiveSystemPrompt, (effectiveSystemPrompt.Id, PromptSlotKind.System));
			generalContext["specialization"] =
				effectiveSpecialization.UseCustomSpecialization ? effectiveSpecialization.CustomSpecialization :
				RenderPromptPart(slotElementManager, effectiveSpecialization, (effectiveSpecialization.Id, PromptSlotKind.Specialization));
			generalContext["persona"] =
				effectivePersona.UseCustomPersona ? effectivePersona.CustomPersona :
				RenderPromptPart(slotElementManager, effectivePersona, (effectivePersona.Id, PromptSlotKind.Persona));
			generalContext["components"] = effectiveComponents
				.Select(c => RenderPromptPart(componentManager, c, c.Id))
				.Where(c => !string.IsNullOrWhiteSpace(c))
				.ToArray();
			generalContext["assistant_nickname"] = effectivePersona.Nickname;
			generalContext["skills"] = skillsetBuilder.GetAddonsForAgent(agent).Where(s => !(s.Hidden ?? false)).Select(s => new
			{
				name = s.Name,
				description = s.Description,
				path = s.Path,
				body = s.InjectionMode is SkillInjectionMode.Full ? s.BodyGetter(s) : null
			});
			generalContext["sub_agents"] = subAgentsetCollector.GetAddonsForAgent(agent).Where(s => !(s.Hidden ?? false)).Select(s => new
			{
				name = s.Name,
				description = s.Description
			});
			generalContext["memory_blocks"] = effectiveChatMemoryOptions.EnableMemory && effectiveChatMemoryOptions.ManualControlEnabled && agent.Memory.EnableMemory
				? agent.Memory.GetEnabledBlocks(chatSettings.Settings)
					.Select(b => new
					{
						name = b.Block.Name,
						can_read = b.Attachment.AllowsReading(),
						can_write = b.Attachment.AllowsWriting(),
						facts_enabled = b.Block.FactsEnabled,
						logs_enabled = b.Block.LogsEnabled,
						description = b.Block.Description
					})
				: null;

			return new CorePromptSectionState
			{
				Text = template.Render(generalContext, functions)
			};
		}
	}

	/// <summary>
	/// Renders the core prompt section state into a snapshot (text only).
	/// </summary>
	[ChatService(typeof(IPromptSectionStateRenderer<CorePromptSectionState>))]
	public class CorePromptStateRenderer : IPromptSectionStateRenderer<CorePromptSectionState>
	{
		/// <inheritdoc/>
		public SystemPromptSnapshot Render(CorePromptSectionState state) => state.Text;
	}

	/// <summary>
	/// Delta provider of the core prompt section (stub: no deltas yet).
	/// </summary>
	[ChatService(typeof(IPromptSectionDeltaProvider<CorePromptSectionState, CorePromptSectionDelta>))]
	public class CorePromptDeltaProvider : IPromptSectionDeltaProvider<CorePromptSectionState, CorePromptSectionDelta>
	{
		/// <inheritdoc/>
		public CorePromptSectionDelta? CalculateDelta(CorePromptSectionState? anchorState,
			IEnumerable<CorePromptSectionDelta> existingDeltas, CorePromptSectionState? actualState,
			EffectiveChatContext context) => null;
	}

	/// <summary>
	/// Delta renderer of the core prompt section (stub: no deltas yet).
	/// </summary>
	[ChatService(typeof(IPromptSectionDeltaRenderer<CorePromptSectionDelta>))]
	public class CorePromptDeltaRenderer : IPromptSectionDeltaRenderer<CorePromptSectionDelta>
	{
		/// <inheritdoc/>
		public string Render(CorePromptSectionDelta delta) => string.Empty;
	}

	/// <summary>
	/// The core prompt section (main part of the system prompt).
	/// </summary>
	public class CorePromptSection(IServiceProvider services)
		: PromptAnchoredSectionBase<CorePromptSectionState, CorePromptSectionDelta>(services)
	{
	}

	[ChatService(typeof(PromptContextNativeProvider))]
	public class CorePromptSectionProvider : PromptContextNativeProvider
	{
		public CorePromptSectionProvider(IServiceProvider services)
		{
			AddContext(new PromptContextInfo
			{
				Name = "core",
				Order = 0,
				Description = string.Empty,
				IsFixed = true,
				Provider = new CorePromptSection(services)
			});
		}
	}
}
