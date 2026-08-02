using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Desktop.Execution
{
	[Service(typeof(IProcessDispatcher))]
	public class ProcessDispatcher : IProcessDispatcher
	{
		private RangeObservableCollection<ProcessDescriptor> _processes;
		public ReadOnlyObservableCollection<ProcessDescriptor> Processes { get; }

		public ProcessDispatcher()
		{
			_processes = [];
			Processes = new(_processes);
		}

		public void OnProcessStart(ProcessDescriptor descriptor)
		{
			_processes.Add(descriptor);
		}

		public void OnProcessEnd(ProcessDescriptor descriptor)
		{
			_processes.Remove(descriptor);
		}
	}
}
