using AngleSharp.Common;
using LLMDesktopAssistant.Core.LLM.Domain;
using LLMDesktopAssistant.Core.Services;
using LLMDesktopAssistant.Core.ToolModules;
using Microsoft.Extensions.DependencyInjection;

namespace LLMDesktopAssistant.Core.LLM.Services.Tools
{
	/// <summary>
	/// The default implementation of the <see cref="IToolsetBuildingService"/> interface.
	/// </summary>
	public class ToolsetBuildingService(
		Chat chat,
		IMCPManagementService mcpManager,
		IServiceProvider services
		) : IToolsetBuildingService
	{
		public IEnumerable<ToolInfo> BuildTools()
		{
			if (!chat.Settings.EnableTools)
				return [];

			var tools = GetAvailableTools();
			var result = new List<ToolInfo>();

			var changes = chat.Settings.ToolChanges.ToDictionary(c => c.ToolName, c => c);
			foreach (var toolInfo in tools)
			{
				if (changes.TryGetValue(toolInfo.Tool.Name, out var change))
				{
					if (change.Enabled ?? toolInfo.Enabled)
						result.Add(new ToolInfo
						{
							Tool = toolInfo.Tool,
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
				.Concat(chat.AdditionalToolModules ?? [])
				.Concat(mcpManager.GetMCPToolModules())

				.SelectMany(m => m.GetTools()
					.Select(t =>
					{
						return new ToolInfo
						{
							Tool = t.Tool,
							Category = t.Category,
							DisplayName = t.DisplayName,
							Source = t.Source,
							Enabled = m.Enabled && t.Enabled,
							AskForConfirmation = t.AskForConfirmation
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