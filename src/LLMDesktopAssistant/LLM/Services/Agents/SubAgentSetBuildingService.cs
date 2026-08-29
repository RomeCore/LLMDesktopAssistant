using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Agents.SubAgents;
using LLMDesktopAssistant.LLM.Services.Prompting;
using LLMDesktopAssistant.Prompting.ContextExpanders;
using LLMDesktopAssistant.Prompting.Management;
using LLMDesktopAssistant.Prompting.Plugins;
using LLTSharp;

namespace LLMDesktopAssistant.LLM.Services.Agents
{
	[ChatService(typeof(ISubAgentSetBuildingService))]
	public class SubAgentSetBuildingService(
		IChatSettingsService chatSettings,
		ISubAgentLocator subAgentLocator,
		ISubAgentLoader subAgentLoader,
		IPromptSubAgentManager subAgentManager,
		IEnumerable<IPromptSystemContextExpander> promptSystemContextExpanders,
		IEnumerable<IPromptTemplatePlugin> promptTemplatePlugins
	) : ISubAgentSetBuildingService
	{
		public IEnumerable<SubAgentInfo> GetAvailableSubAgents()
		{
			var subAgentFiles = subAgentLocator.LocateSubAgentFiles();

			List<SubAgentInfo> subAgents = [];

			var promptSubAgents = subAgentManager.GetAll().ToList();
			if (promptSubAgents.Count > 0)
			{
				subAgents.AddRange(promptSubAgents.Select(sp =>
				{
					return new SubAgentInfo
					{
						Name = sp.Name,
						Description = sp.Description ?? string.Empty,
						SystemPromptGetter = new(si =>
						{
							var context = new Dictionary<string, object?>();
							foreach (var expander in promptSystemContextExpanders)
								expander.ExpandPromptContext(context);
							var templateFunctions = new TemplateFunctionSet(promptTemplatePlugins.SelectMany(p => p.GetTemplateFunctions()));

							if (sp.ParameterSchema is not null)
							{
								if (si.Change is not null)
								{
									si.Change.Parameters = sp.ParameterSchema.Root.CreateOrFixValue(si.Change.Parameters, []);
									context["params"] = si.Change.Parameters.GetTemplateDataAccessor();
								}
								else
								{
									var @params = sp.ParameterSchema.Root.CreateOrFixValue(null, []);
									context["params"] = @params.GetTemplateDataAccessor();
								}
							}
							return sp.EffectiveTemplate.Render(context, templateFunctions).ToString() ?? string.Empty;
						}),
						Source = SubAgentSource.Template,
						TemplateSource = sp.Source,
						ParameterSchema = sp.ParameterSchema
					};
				}));
			}
			subAgents.AddRange(subAgentLoader.Load(subAgentFiles));

			return subAgents
				.GroupBy(s => s.Name)
				.Select(g =>
				{
					ImmutableList<SubAgentInfo>.Builder? overridesBuilder = null;
					SubAgentInfo? last = null;
					foreach (var subAgent in g)
					{
						if (last is not null)
						{
							overridesBuilder ??= ImmutableList.CreateBuilder<SubAgentInfo>();
							overridesBuilder.Add(last);
						}
						last = subAgent;
					}
					if (overridesBuilder == null)
						return last!;
					return new SubAgentInfo
					{
						Name = last!.Name,
						Description = last.Description,
						SystemPromptGetter = last.SystemPromptGetter,
						Source = last.Source,
						TemplateSource = last.TemplateSource,
						Path = last.Path,
						Metadata = last.Metadata,
						AdditionalMetadata = last.AdditionalMetadata,
						AllowedTools = last.AllowedTools,
						AvailableTools = last.AvailableTools,
						DisallowedTools = last.DisallowedTools,
						Skills = last.Skills,
						SubAgents = last.SubAgents,
						MemoryBlocks = last.MemoryBlocks,
						Tags = last.Tags,
						AdditionalProperties = last.AdditionalProperties,
						Model = last.Model,
						Enabled = last.Enabled,
						ParameterSchema = last.ParameterSchema,
						Overrides = overridesBuilder.ToImmutable()
					};
				});
		}

		public IEnumerable<SubAgentInfo> GetSubAgentsForAgent(ChatAgentDescriptor agent)
		{
			if (!chatSettings.Settings.SubAgents.EnableSubAgents)
				return [];

			var settings = agent.SubAgents;
			if (!settings.EnableSubAgents)
				return [];

			var subAgents = GetAvailableSubAgents();
			var result = new List<SubAgentInfo>();

			var subAgentset = settings.GetEffectiveSubAgentset(chatSettings.Settings);

			var changes = subAgentset.SubAgentChanges.ToDictionary(c => c.SubAgentName, c => c);
			foreach (var subAgentInfo in subAgents)
			{
				if (subAgentInfo.Diagnostic?.IsFatal == true)
					continue;

				if (changes.TryGetValue(subAgentInfo.Name, out var change))
				{
					if (change.Enabled ?? subAgentInfo.Enabled ?? subAgentset.SubAgentsEnabledByDefault)
						result.Add(new SubAgentInfo
						{
							Name = subAgentInfo.Name,
							Source = subAgentInfo.Source,
							TemplateSource = subAgentInfo.TemplateSource,
							Description = subAgentInfo.Description,
							SystemPromptGetter = subAgentInfo.SystemPromptGetter,
							Path = subAgentInfo.Path,
							Metadata = subAgentInfo.Metadata,
							AdditionalMetadata = subAgentInfo.AdditionalMetadata,
							AllowedTools = subAgentInfo.AllowedTools,
							AvailableTools = subAgentInfo.AvailableTools,
							DisallowedTools = subAgentInfo.DisallowedTools,
							Skills = subAgentInfo.Skills,
							SubAgents = subAgentInfo.SubAgents,
							MemoryBlocks = subAgentInfo.MemoryBlocks,
							Tags = subAgentInfo.Tags,
							AdditionalProperties = subAgentInfo.AdditionalProperties,
							Enabled = true,
							Model = change.Model ?? subAgentInfo.Model,
							Change = change,
							ParameterSchema = subAgentInfo.ParameterSchema,
							Overrides = subAgentInfo.Overrides
						});
				}
				else
				{
					if (subAgentInfo.Enabled ?? subAgentset.SubAgentsEnabledByDefault)
						result.Add(subAgentInfo);
				}
			}

			return result;
		}
	}
}
