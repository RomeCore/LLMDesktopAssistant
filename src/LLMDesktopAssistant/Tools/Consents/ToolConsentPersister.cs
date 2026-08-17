using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Services;

namespace LLMDesktopAssistant.Tools.Consents
{
	/// <summary>
	/// Persists user consent decisions into the agent toolset configuration.
	/// </summary>
	public static class ToolConsentPersister
	{
		/// <summary>
		/// Writes an "always" consent decision into the <see cref="ToolChange"/> of the agent's effective
		/// toolset, so the tool approval level is updated persistently.
		/// </summary>
		/// <param name="agent">The agent whose toolset is updated.</param>
		/// <param name="chatSettings">The chat settings service used to resolve the effective toolset.</param>
		/// <param name="toolName">The name of the tool.</param>
		/// <param name="approved"><see langword="true"/> to always approve the tool, <see langword="false"/> to always disallow it.</param>
		public static void MemorizeAlways(ChatAgentDescriptor agent, IChatSettingsService chatSettings, string toolName, bool approved)
		{
			var toolset = agent.Tools.GetEffectiveToolset(chatSettings.Settings).GetEffectiveConfiguration();
			var change = toolset.ToolChanges.FirstOrDefault(c => c.ToolName == toolName);
			if (change == null)
			{
				change = new ToolChange { ToolName = toolName };
				toolset.ToolChanges.Add(change);
			}

			change.ApprovalLevel = approved ? ToolApprovalLevel.AlwaysApprove : ToolApprovalLevel.AlwaysDisallow;
		}

		/// <summary>
		/// Gets the persisted "always" consent decisions of the agent's effective toolset,
		/// that is the <see cref="ToolChange"/> entries with
		/// <see cref="ToolApprovalLevel.AlwaysApprove"/> or <see cref="ToolApprovalLevel.AlwaysDisallow"/>.
		/// </summary>
		/// <param name="agent">The agent whose toolset is inspected.</param>
		/// <param name="chatSettings">The chat settings service used to resolve the effective toolset.</param>
		/// <returns>The "always" tool changes of the agent.</returns>
		public static IEnumerable<ToolChange> GetAlwaysChanges(ChatAgentDescriptor agent, IChatSettingsService chatSettings)
		{
			var toolset = agent.Tools.GetEffectiveToolset(chatSettings.Settings).GetEffectiveConfiguration();
			return toolset.ToolChanges.Where(c =>
				c.ApprovalLevel is ToolApprovalLevel.AlwaysApprove or ToolApprovalLevel.AlwaysDisallow);
		}

		/// <summary>
		/// Forgets a persisted "always" consent decision by resetting the approval level of the
		/// given tool change to <see langword="null"/>, keeping the other tool changes intact.
		/// </summary>
		/// <param name="change">The tool change to reset.</param>
		public static void ForgetAlways(ToolChange change)
		{
			change.ApprovalLevel = null;
		}
	}
}
