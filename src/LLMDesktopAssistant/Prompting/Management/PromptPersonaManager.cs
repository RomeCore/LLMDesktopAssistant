using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Settings;
using LLTSharp.Metadata;

namespace LLMDesktopAssistant.Prompting.Management
{
	/// <summary>
	/// Manages <see cref="PromptPersona"/> instances imported from templates and configuration.
	/// </summary>
	[ChatService(typeof(IPromptPersonaManager))]
	[ChatService(typeof(IImportablePromptPartManager))]
	public class PromptPersonaManager : PromptPartManagerBase<Guid, PromptPersona>, IPromptPersonaManager
	{
		/// <inheritdoc/>
		public override string TemplateType => "persona";

		/// <inheritdoc/>
		protected override bool RequiresGuid => true;

		/// <inheritdoc/>
		protected override bool RequiresStrId => false;

		/// <inheritdoc/>
		protected override PromptPartConfigurationBase<PromptPersona> Configuration =>
			SettingsManager.Get<PromptPersonasConfiguration>();

		/// <inheritdoc/>
		protected override Guid GetKey(PromptPersona part) => part.Guid;

		/// <inheritdoc/>
		protected override void PopulatePart(PromptPersona part, IMetadataCollection metadata)
		{
		}
	}
}
