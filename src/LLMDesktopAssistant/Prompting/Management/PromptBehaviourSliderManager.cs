using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Settings;
using LLTSharp.Metadata;

namespace LLMDesktopAssistant.Prompting.Management
{
	/// <summary>
	/// Manages <see cref="PromptBehaviourSlider"/> instances imported from templates and configuration.
	/// </summary>
	[ChatService(typeof(IPromptBehaviourSliderManager))]
	[ChatService(typeof(IImportablePromptPartManager))]
	public class PromptBehaviourSliderManager : PromptPartManagerBase<Guid, PromptBehaviourSlider>, IPromptBehaviourSliderManager
	{
		/// <inheritdoc/>
		public override string TemplateType => "slider";

		/// <inheritdoc/>
		protected override bool RequiresGuid => true;

		/// <inheritdoc/>
		protected override bool RequiresStrId => false;

		/// <inheritdoc/>
		protected override PromptPartConfigurationBase<PromptBehaviourSlider> Configuration =>
			SettingsManager.Get<PromptBehaviourSlidersConfiguration>();

		/// <inheritdoc/>
		protected override Guid GetKey(PromptBehaviourSlider part) => part.Guid;

		/// <inheritdoc/>
		protected override void PopulatePart(PromptBehaviourSlider part, IMetadataCollection metadata)
		{
			var hintsRaw = metadata.TryGetAdditional<object?[]>("hints") ?? [];
			var sliderMin = (int)metadata.TryGetAdditional<double>("slider_min");
			var sliderMax = (int)metadata.TryGetAdditional<double>("slider_max");
			var sliderDefault = (int)metadata.TryGetAdditional<double>("slider_default");

			if (sliderMin >= sliderMax || sliderDefault < sliderMin || sliderDefault > sliderMax)
			{
				part.ExpandDiagnostic(new PromptPartDiagnostic
				{
					IsFatal = true,
					Code = PromptPartDiagnosticCode.InvalidSliderRange,
					Messages = [$"Slider range is invalid. Min: {sliderMin}, Max: {sliderMax}, Default: {sliderDefault}"]
				});
				return;
			}

			var hints = ImmutableDictionary.CreateBuilder<int, string>();
			var hintsLength = sliderMin != 0 && sliderMax != 0 ? sliderMax - sliderMin + 1 : hintsRaw.Length;

			if (hintsLength != hintsRaw.Length)
			{
				part.ExpandDiagnostic(new PromptPartDiagnostic
				{
					IsFatal = true,
					Code = PromptPartDiagnosticCode.InvalidSliderHints,
					Messages = [$"Invalid slider hints: length of hint array must be equal to length of range, " +
						$"but currently hints:{hintsRaw.Length} != range:{hintsLength}."]
				});
				return;
			}

			for (int i = 0; i < hintsLength; i++)
				hints[i + sliderMin] = hintsRaw[i]?.ToString() ?? string.Empty;

			part.Titles = hints.ToImmutable();
			part.MinimumValue = sliderMin;
			part.MaximumValue = sliderMax;
			part.DefaultValue = sliderDefault;
		}
	}
}
