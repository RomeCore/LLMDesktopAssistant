using LLMDesktopAssistant.LLM.Domain;

namespace LLMDesktopAssistant.LLM.MVVM.Additional.Context
{
	[ViewModelFor(typeof(ContextShieldView))]
	public class ContextShieldViewModel : AdditionalMessageViewModel
	{
		/// <summary>
		/// Display order — shown below message content.
		/// </summary>
		public override int Order => 100;
	}
}
