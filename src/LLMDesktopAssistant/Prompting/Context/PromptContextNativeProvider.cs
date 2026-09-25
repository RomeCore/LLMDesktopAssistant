namespace LLMDesktopAssistant.Prompting.Context
{
	public abstract class PromptContextNativeProvider
	{
		private readonly List<PromptContextInfo> _contexts = [];

		protected void AddContext(PromptContextInfo context)
		{
			context.OverrideOrder = 1;
			context.Freeze();
			_contexts.Add(context);
		}

		public virtual IEnumerable<PromptContextInfo> GetContexts()
		{
			return _contexts;
		}
	}
}
