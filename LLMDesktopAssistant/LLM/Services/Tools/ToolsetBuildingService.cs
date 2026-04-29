using AngleSharp.Common;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Tools;
using Microsoft.Extensions.DependencyInjection;

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
							DescriptionGetter = toolInfo.DescriptionGetter,
							ArgumentSchema = toolInfo.ArgumentSchema,
							Executor = toolInfo.Executor,
							Category = toolInfo.Category,
							DisplayName = toolInfo.DisplayName,
							Source = toolInfo.Source,
							Enabled = true,
							AskForConfirmation = change.AskForConfirmation ?? toolInfo.AskForConfirmation
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

		public IEnumerable<ToolInfo> GetAvailableTools()
		{
			var metatoolManager = services.GetService<IMetaToolManagementService>();

			return services.GetServices<ToolModule>()

				// Tool modules
				.Concat(chat.AdditionalTools ?? [])
				.Concat(mcpManager.GetMCPTools())

				.SelectMany(m => m.GetTools()
					.Select(toolInfo =>
					{
						return new ToolInfo
						{
							Name = toolInfo.Name,
							DescriptionGetter = toolInfo.DescriptionGetter,
							ArgumentSchema = toolInfo.ArgumentSchema,
							Executor = toolInfo.Executor,
							Category = toolInfo.Category,
							DisplayName = toolInfo.DisplayName,
							Source = toolInfo.Source,
							Enabled = m.Enabled && toolInfo.Enabled,
							AskForConfirmation = toolInfo.AskForConfirmation
						};
					}))

				// Tools
				.Concat(metatoolManager?.GetMetaTools() ?? [])

				// Select the last tool of each name (to avoid duplicates)
				.GroupBy(t => t.Tool.Name)
				.Select(t => t.Last())

				.ToList();
		}
	}
}