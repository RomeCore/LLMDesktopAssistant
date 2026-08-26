using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Settings;
using LLTSharp.Metadata;

namespace LLMDesktopAssistant.Prompting.Management
{
	/// <summary>
	/// Manages <see cref="PromptComponent"/> instances imported from templates and configuration.
	/// </summary>
	[ChatService(typeof(IPromptComponentManager))]
	[ChatService(typeof(IImportablePromptPartManager))]
	public class PromptComponentManager : PromptPartManagerBase<Guid, PromptComponent>, IPromptComponentManager
	{
		/// <inheritdoc/>
		public override string TemplateType => "component";

		/// <inheritdoc/>
		protected override bool RequiresGuid => true;

		/// <inheritdoc/>
		protected override bool RequiresStrId => false;

		/// <inheritdoc/>
		protected override PromptPartConfigurationBase<PromptComponent> Configuration =>
			SettingsManager.Get<PromptComponentsConfiguration>();

		/// <inheritdoc/>
		protected override Guid GetKey(PromptComponent part) => part.Guid;

		/// <inheritdoc/>
		protected override void PopulateFromMetadata(PromptComponent part, IMetadataCollection metadata, bool isLocalized)
		{
		}

		/// <inheritdoc/>
		protected override void PopulateLocalized(PromptComponent original, PromptComponent localized)
		{
		}
	}
}
