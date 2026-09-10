namespace LLMDesktopAssistant.LLM.MVVM.Additional.Context
{
	[ViewModelFor(typeof(ContextShieldView))]
	public class ContextShieldViewModel : AdditionalChatData
	{
		/// <summary>
		/// Display order — shown below message content.
		/// </summary>
		public override int Order => 100;
	}
}
