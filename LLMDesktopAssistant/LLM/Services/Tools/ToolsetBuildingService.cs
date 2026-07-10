using AngleSharp.Common;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Agents;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Tools;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Text.Json.Serialization.Metadata;

namespace LLMDesktopAssistant.LLM.Services.Tools
{
	/// <summary>
	/// The default implementation of the <see cref="IToolsetBuildingService"/> interface.
	/// </summary>
	[ChatService(typeof(IToolsetBuildingService))]
	public class ToolsetBuildingService(
		Chat chat,
		IMCPManagementService mcpManager,
		IAgentManagementService agentSettings,
		IServiceProvider services
		) : IToolsetBuildingService
	{
		public IEnumerable<ToolInfo> BuildTools(Guid agentId)
		{
			if (!chat.Settings.Tools.EnableTools)
				return [];

			var settings = agentSettings.GetAgentDescriptor(agentId).Tools;
			if (!settings.EnableTools)
				return [];

			var tools = GetAvailableTools();
			var result = new List<ToolInfo>();

			var changes = settings.ToolChanges.ToDictionary(c => c.ToolName, c => c);
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
/*
			Log.Information("Tool list:\n{Tools}", string.Join("\n", result.Select(t => $"""
				Tool: {t.Name}
				Description: {t.DescriptionGetter()}
				Arguments: {t.ArgumentSchema.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true, TypeInfoResolver = new DefaultJsonTypeInfoResolver() })}
				""")));
*/
			return result;
		}

		public IEnumerable<ToolInfo> GetAvailableTools()
		{
			var metatoolManager = services.GetService<IMetaToolManagementService>();

			return services.GetServices<ToolModule>()

				// Tool modules
				.Concat(chat.AdditionalTools ?? [])
				.Concat(mcpManager.GetMCPTools())

				.SelectMany(m => m.GetTools())

				// Tools
				.Concat(metatoolManager?.GetMetaTools() ?? [])

				// Select the last tool of each name (to avoid duplicates)
				.GroupBy(t => t.Tool.Name)
				.Select(t => t.Last())

				.ToList();
		}
	}
}