using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Prompting.Context
{
	public class PromptContextInfo : AddonChangedBase<PromptContextInfo, PromptContextChange>
	{
		/// <summary>
		/// The variability of this prompt context.
		/// </summary>
		public PromptSectionVariability Variability
		{
			get;
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// Gets the provider implementation for this prompt context.
		/// </summary>
		public IPromptContextProvider Provider
		{
			get;
			set => SetProperty(ref field, value);
		} = null!;

		protected override void ValidatePropertiesCore(AppendOnlyList<string> errors)
		{
			base.ValidatePropertiesCore(errors);

			if (Provider == null)
				throw new InvalidOperationException("Provider is required.");
		}
	}
}
