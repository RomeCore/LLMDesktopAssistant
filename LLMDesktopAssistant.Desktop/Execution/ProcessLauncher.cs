using LLMDesktopAssistant.Services;

namespace LLMDesktopAssistant.Desktop.Execution
{
	[Service(typeof(IProcessLauncher))]
	public class ProcessLauncher : IProcessLauncher
	{
		private IProcessDispatcher _dispatcher;

		public ProcessLauncher(IProcessDispatcher dispatcher)
		{
			_dispatcher = dispatcher;
		}

		public ProcessDescriptor Launch(ProcessLaunchParameters parameters, CancellationToken cancellationToken = default)
		{
			if (parameters.RunInTerminal)
				return LaunchTerminal(parameters, cancellationToken);
			else
				return LaunchNonTerminal(parameters, cancellationToken);
		}

		private static ProcessDescriptor LaunchTerminal(ProcessLaunchParameters parameters, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		private static ProcessDescriptor LaunchNonTerminal(ProcessLaunchParameters parameters, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}
	}
}
