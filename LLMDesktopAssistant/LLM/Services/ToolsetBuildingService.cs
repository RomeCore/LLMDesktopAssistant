using AngleSharp.Common;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Modules;
using LLMDesktopAssistant.ToolModules;
using Microsoft.Extensions.DependencyInjection;

namespace LLMDesktopAssistant.LLM.Services
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
			var toolModules = ModuleManager.GetAll<ToolModule>();

			return toolModules
				.Concat(chat.AdditionalToolModules ?? [])
				.Concat(services.GetServices<ToolModule>())
				.Concat(mcpManager.GetMCPToolModules())
				.SelectMany(m => m.GetTools()
					.Select(t =>
					{
						return new ToolInfo
						{
							Tool = t.Tool,
							Category = t.Category,
							Enabled = m.Enabled && t.Enabled,
							AskForConfirmation = t.AskForConfirmation
						};
					}))
				.ToList();
		}
	}
}