namespace LLMDesktopAssistant.LLM.Services
{
	/// <summary>
	/// Base class for <see cref="IChatExecutionHook"/> implementations with no-op
	/// default implementations, so hooks only override the events they need.
	/// </summary>
	public abstract class ChatExecutionHookBase : IChatExecutionHook
	{
		/// <inheritdoc />
		public virtual int Order => 0;

		/// <inheritdoc />
		public virtual Task OnResponseCompletedAsync(ChatExecutionHookContext context, CancellationToken cancellationToken = default)
			=> Task.CompletedTask;

		/// <inheritdoc />
		public virtual Task OnExecutionFinishedAsync(ChatExecutionHookContext context, CancellationToken cancellationToken = default)
			=> Task.CompletedTask;
	}
}
