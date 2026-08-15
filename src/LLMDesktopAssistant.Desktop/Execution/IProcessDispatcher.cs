using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Desktop.Execution
{
	public interface IProcessDispatcher
	{
		ReadOnlyObservableCollection<ProcessDescriptor> Processes { get; }

		void OnProcessStart(ProcessDescriptor descriptor);

		void OnProcessEnd(ProcessDescriptor descriptor);
	}
}
