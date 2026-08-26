using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Settings;
using LLTSharp.Metadata;

namespace LLMDesktopAssistant.Prompting.Management
{
	/// <summary>
	/// Manages <see cref="PromptSpecialization"/> instances imported from templates and configuration.
	/// </summary>
	[ChatService(typeof(IPromptSpecializationManager))]
	[ChatService(typeof(IImportablePromptPartManager))]
	public class PromptSpecializationManager : PromptPartManagerBase<Guid, PromptSpecialization>, IPromptSpecializationManager
	{
		/// <inheritdoc/>
		public override string TemplateType => "specialization";

		/// <inheritdoc/>
		protected override bool RequiresGuid => true;

		/// <inheritdoc/>
		protected override bool RequiresStrId => false;

		/// <inheritdoc/>
		protected override PromptPartConfigurationBase<PromptSpecialization> Configuration =>
			SettingsManager.Get<PromptSpecializationsConfiguration>();

		/// <inheritdoc/>
		protected override Guid GetKey(PromptSpecialization part) => part.Guid;

		/// <inheritdoc/>
		protected override void PopulatePart(PromptSpecialization part, IMetadataCollection metadata)
		{
		}
	}
}
