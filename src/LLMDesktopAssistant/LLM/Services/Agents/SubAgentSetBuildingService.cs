using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Agents.SubAgents;
using LLMDesktopAssistant.Prompting.ContextExpanders;
using LLMDesktopAssistant.Prompting.Management;
using LLMDesktopAssistant.Prompting.Plugins;
using LLTSharp;

namespace LLMDesktopAssistant.LLM.Services.Prompting
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
				var context = new Dictionary<string, object?>();
				foreach (var expander in promptSystemContextExpanders)
					expander.ExpandPromptContext(context);
				var templateFunctions = new TemplateFunctionSet(promptTemplatePlugins.SelectMany(p => p.GetTemplateFunctions()));

				subAgents.AddRange(promptSubAgents.Select(s =>
				{
					return new SubAgentInfo
					{
						Name = s.Name,
						Description = s.Description ?? string.Empty,
						SystemPromptGetter = new(() => s.Template.Template.Render(context, templateFunctions).ToString() ?? string.Empty),
						Source = SubAgentSource.Template
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

			var changes = settings.GetEffectiveSubAgentChanges(chatSettings.Settings).ToDictionary(c => c.SubAgentName, c => c);
			foreach (var subAgentInfo in subAgents)
			{
				if (subAgentInfo.Diagnostic?.IsFatal == true)
					continue;

				if (changes.TryGetValue(subAgentInfo.Name, out var change))
				{
					if (change.Enabled ?? subAgentInfo.Enabled)
						result.Add(new SubAgentInfo
						{
							Enabled = true,
							Model = change.Model ?? subAgentInfo.Model,
							Name = subAgentInfo.Name,
							Source = subAgentInfo.Source,
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
						});
				}
				else
				{
					if (subAgentInfo.Enabled)
						result.Add(subAgentInfo);
				}
			}

			return result;
		}
	}
}
