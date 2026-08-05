using AngleSharp.Common;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Tools.Meta;

namespace LLMDesktopAssistant.LLM.Services.Tools
{
	/// <summary>
	/// The default implementation of the <see cref="IToolsetBuildingService"/> interface.
	/// </summary>
	[ChatService(typeof(IToolsetBuildingService))]
	public class ToolsetBuildingService(
		Chat chat,
		IMCPManagementService mcpManager,
		IServiceProvider services
		) : IToolsetBuildingService
	{
		public IEnumerable<ToolInfo> GetAvailableTools()
		{
			var metatoolManager = services.GetService<IMetaToolManagementService>();

			return services.GetServices<ToolModule>()

				.Concat(chat.AdditionalTools ?? [])
				.Concat(mcpManager.GetMCPTools())

				.SelectMany(m => m.GetTools())

				.Concat(metatoolManager?.GetMetaTools() ?? [])

				.GroupBy(t => t.Tool.Name)
				.Select(g =>
				{
					ImmutableList<ToolInfo>.Builder? overridesBuilder = null;
					ToolInfo? last = null;
					foreach (var skill in g)
					{
						if (last is not null)
						{
							overridesBuilder ??= ImmutableList.CreateBuilder<ToolInfo>();
							overridesBuilder.Add(last);
						}
						last = skill;
					}
					if (overridesBuilder == null)
						return last!;
					return new ToolInfo
					{
						Name = last!.Name,
						Aliases = last.Aliases,
						DescriptionGetter = last.DescriptionGetter,
						ArgumentSchema = last.ArgumentSchema,
						OutputSchema = last.OutputSchema,
						StreamingArgumentsAnalyser = last.StreamingArgumentsAnalyser,
						PreviewExecutor = last.PreviewExecutor,
						DefaultExpectedBehaviour = last.DefaultExpectedBehaviour,
						DefaultSelfHandledDecisions = last.DefaultSelfHandledDecisions,
						Executor = last.Executor,
						SynchronizationGroup = last.SynchronizationGroup,
						Category = last.Category,
						DisplayName = last.DisplayName,
						Source = last.Source,
						Enabled = last.Enabled,
						ApprovalLevel = last.ApprovalLevel,
						Overrides = overridesBuilder.ToImmutable()
					};
				});
		}

		public IEnumerable<ToolInfo> GetToolsForAgent(ChatAgentDescriptor agent)
		{
			if (!chat.Settings.Tools.EnableTools)
				return [];

			var settings = agent.Tools;
			if (!settings.EnableTools)
				return [];

			var tools = GetAvailableTools();
			var result = new List<ToolInfo>();

			var toolset = settings.GetEffectiveToolset(chat.Settings).GetEffectiveConfiguration();
			var changes = toolset.ToolChanges.ToDictionary(c => c.ToolName, c => c);
			foreach (var toolInfo in tools)
			{
				if (changes.TryGetValue(toolInfo.Tool.Name, out var change))
				{
					if (change.Enabled ?? toolInfo.Enabled)
						result.Add(new ToolInfo
						{
							Name = toolInfo.Name,
							Aliases = toolInfo.Aliases,
							DescriptionGetter = toolInfo.DescriptionGetter,
							ArgumentSchema = toolInfo.ArgumentSchema,
							OutputSchema = toolInfo.OutputSchema,
							StreamingArgumentsAnalyser = toolInfo.StreamingArgumentsAnalyser,
							PreviewExecutor = toolInfo.PreviewExecutor,
							DefaultExpectedBehaviour = toolInfo.DefaultExpectedBehaviour,
							DefaultSelfHandledDecisions = toolInfo.DefaultSelfHandledDecisions,
							Executor = toolInfo.Executor,
							SynchronizationGroup = toolInfo.SynchronizationGroup,
							Category = toolInfo.Category,
							DisplayName = toolInfo.DisplayName,
							Source = toolInfo.Source,
							Enabled = true,
							ApprovalLevel = change.ApprovalLevel ?? toolInfo.ApprovalLevel
						});
				}
				else
				{
					if (toolInfo.Enabled)
						result.Add(toolInfo);
				}
			}

			return result;
		}
	}
}