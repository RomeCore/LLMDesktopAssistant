using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Settings;
using LLTSharp.Metadata;

namespace LLMDesktopAssistant.Prompting.Management
{
	/// <summary>
	/// Manages <see cref="PromptSkill"/> instances imported from templates and configuration.
	/// </summary>
	[ChatService(typeof(IPromptSkillManager))]
	[ChatService(typeof(IImportablePromptPartManager))]
	public class PromptSkillManager : PromptPartManagerBase<string, PromptSkill>, IPromptSkillManager
	{
		/// <inheritdoc/>
		public override string TemplateType => "skill";

		/// <inheritdoc/>
		protected override bool RequiresGuid => false;

		/// <inheritdoc/>
		protected override bool RequiresStrId => true;

		/// <inheritdoc/>
		protected override PromptPartConfigurationBase<PromptSkill> Configuration =>
			SettingsManager.Get<PromptSkillsConfiguration>();

		/// <inheritdoc/>
		protected override string GetKey(PromptSkill part) => part.StrId;

		/// <inheritdoc/>
		protected override void PopulatePart(PromptSkill part, IMetadataCollection metadata)
		{
			// Description is required for all skills.
			if (string.IsNullOrWhiteSpace(part.Description))
				part.ExpandDiagnostic(new PromptPartDiagnostic
				{
					Code = PromptPartDiagnosticCode.MissingDescription,
					IsFatal = true
				});
		}
	}
}
