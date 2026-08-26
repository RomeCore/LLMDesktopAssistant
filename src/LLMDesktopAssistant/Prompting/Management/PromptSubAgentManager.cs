using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Settings;
using LLTSharp.Metadata;

namespace LLMDesktopAssistant.Prompting.Management
{
	/// <summary>
	/// Manages <see cref="PromptSubAgent"/> instances imported from templates and configuration.
	/// </summary>
	[ChatService(typeof(IPromptSubAgentManager))]
	[ChatService(typeof(IImportablePromptPartManager))]
	public class PromptSubAgentManager : PromptPartManagerBase<string, PromptSubAgent>, IPromptSubAgentManager
	{
		/// <inheritdoc/>
		public override string TemplateType => "sub-agent";

		/// <inheritdoc/>
		protected override bool RequiresGuid => false;

		/// <inheritdoc/>
		protected override bool RequiresStrId => true;

		/// <inheritdoc/>
		protected override PromptPartConfigurationBase<PromptSubAgent> Configuration =>
			SettingsManager.Get<PromptSubAgentsConfiguration>();

		/// <inheritdoc/>
		protected override string GetKey(PromptSubAgent part) => part.StrId;

		/// <inheritdoc/>
		protected override void PopulatePart(PromptSubAgent part, IMetadataCollection metadata)
		{
			// Description is required for all sub-agents.
			if (string.IsNullOrWhiteSpace(part.Description))
				part.ExpandDiagnostic(new PromptPartDiagnostic
				{
					Code = PromptPartDiagnosticCode.MissingDescription,
					IsFatal = true
				});
		}
	}
}
