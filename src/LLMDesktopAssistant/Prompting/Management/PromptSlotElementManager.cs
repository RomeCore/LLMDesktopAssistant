using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Settings;
using LLTSharp.Metadata;

namespace LLMDesktopAssistant.Prompting.Management
{
	/// <summary>
	/// Manages <see cref="PromptSlotElement"/> instances imported from templates and configuration.
	/// </summary>
	[ChatService(typeof(IPromptSlotElementManager))]
	[ChatService(typeof(IImportablePromptPartManager))]
	public class PromptSlotElementManager : PromptPartManagerBase<(Guid, PromptSlotKind), PromptSlotElement>, IPromptSlotElementManager
	{
		/// <inheritdoc/>
		public override string TemplateType => "slot";

		/// <inheritdoc/>
		protected override bool RequiresGuid => true;

		/// <inheritdoc/>
		protected override bool RequiresStrId => false;

		/// <inheritdoc/>
		protected override PromptPartConfigurationBase<PromptSlotElement> Configuration =>
			SettingsManager.Get<PromptSlotElementsConfiguration>();

		/// <inheritdoc/>
		protected override (Guid, PromptSlotKind) GetKey(PromptSlotElement part) => (part.Guid, part.Kind);

		/// <inheritdoc/>
		protected override void PopulateFromMetadata(PromptSlotElement part, IMetadataCollection metadata, bool isLocalized)
		{
			if (isLocalized)
				return;

			var slot = metadata.TryGetAdditional<string>("slot");

			var slotKind = slot switch
			{
				"system" => PromptSlotKind.System,
				"persona" => PromptSlotKind.Persona,
				"specialization" => PromptSlotKind.Specialization,
				_ => (PromptSlotKind?)null
			};

			if (slot is null)
			{
				part.ExpandDiagnostic(new PromptPartDiagnostic
				{
					IsFatal = true,
					Code = PromptPartDiagnosticCode.InvalidSlotKind,
					Messages = ["Slot kind is not specified. Expected string at 'slot' key."]
				});
				return;
			}

			if (slotKind is null)
			{
				part.ExpandDiagnostic(new PromptPartDiagnostic
				{
					IsFatal = true,
					Code = PromptPartDiagnosticCode.InvalidSlotKind,
					Messages = [$"Invalid slot kind '{slot}'. Expected one of 'system', 'persona', 'specialization'."]
				});
				return;
			}

			part.Kind = slotKind.Value;
		}

		/// <inheritdoc/>
		protected override void PopulateLocalized(PromptSlotElement original, PromptSlotElement localized)
		{
			localized.Kind = original.Kind;
		}
	}
}
