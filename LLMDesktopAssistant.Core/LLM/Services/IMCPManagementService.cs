using LLMDesktopAssistant.Core.LLM.Domain;
using LLMDesktopAssistant.Core.MCP;
using LLMDesktopAssistant.Core.ToolModules;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.Core.LLM.Services
{
	public interface IMCPManagementService
	{
		/// <summary>
		/// Ensures that all current MCP (Message Control Protocol) connections are established and active asynchronously.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		Task EnsureCurrentMCPConnectionsAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieves an array of available MCP tool modules with their associated tool information.
		/// </summary>
		/// <returns>An array of <see cref="ToolInfo"/> objects representing the available MCP tool modules.</returns>
		MCPToolModule[] GetMCPToolModules();
	}
}