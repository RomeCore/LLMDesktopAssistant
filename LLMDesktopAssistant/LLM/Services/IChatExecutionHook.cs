namespace LLMDesktopAssistant.LLM.Services
{
	/// <summary>
	/// A hook that is invoked at specific stages of the chat execution pipeline.
	/// Implementations are chat-scoped services resolved via
	/// <see cref="ChatServiceAttribute"/>; multiple implementations are supported
	/// and are executed in ascending <see cref="Order"/>.
	/// </summary>
	public interface IChatExecutionHook
	{
		/// <summary>
		/// Gets the execution order of the hook. Hooks run in ascending order.
		/// </summary>
		int Order { get; }

		/// <summary>
		/// Called before the LLM generates a response to the user's input and before prompt is built.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		Task OnResponsePrepareAsync(ChatPreExecutionHookContext context, CancellationToken cancellationToken = default);

		/// <summary>
		/// Called after each LLM response cycle is completed. Hooks are awaited
		/// sequentially and isolated from each other: a failure in one hook does not
		/// affect the other hooks or the execution pipeline.
		/// </summary>
		/// <param name="context">The context of the completed response cycle.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		Task OnResponseCompletedAsync(ChatExecutionHookContext context, CancellationToken cancellationToken = default);

		/// <summary>
		/// Called once after the whole agent response chain has finished.
		/// The execution pipeline does not wait for the completion of this method
		/// (fire-and-forget semantics).
		/// </summary>
		/// <param name="context">The context of the finished execution.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		Task OnExecutionFinishedAsync(ChatExecutionHookContext context, CancellationToken cancellationToken = default);
	}
}
