using LLMDesktopAssistant.Services;
using RCLargeLanguageModels.Tasks;

namespace LLMDesktopAssistant.Agents.Tasks
{
	[Service(typeof(IAgentTaskExecutor))]
	public class AgentTaskExecutor : IAgentTaskExecutor
	{
		public AgentTask Execute(AgentTaskLaunchParameters parameters, CancellationToken cancellationToken = default)
		{
			var completionSource = new CompletionSource();

			var task = new AgentTask
			{
				Completion = completionSource.Token,
				CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken),
				LaunchParameters = parameters
			};
			task.Messages.AddRange(parameters.InitialMessages);

			Task.Run(async () =>
			{
				try
				{
					// TODO: Implement task execution logic here.

					completionSource.Complete();
				}
				catch (Exception ex)
				{
					completionSource.Fail(ex);
				}
				finally
				{

				}
			}, CancellationToken.None);

			return task;
		}
	}
}
