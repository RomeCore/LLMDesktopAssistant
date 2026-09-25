namespace LLMDesktopAssistant.Prompting.Context
{
	public abstract class PromptContextNativeProvider
	{
		private readonly List<PromptContextInfo> _contexts = [];

		protected void AddContext(PromptContextInfo context)
		{
			_contexts.Add(context);
		}

		public virtual IEnumerable<PromptContextInfo> GetContexts()
		{
			return _contexts;
		}
	}
}
