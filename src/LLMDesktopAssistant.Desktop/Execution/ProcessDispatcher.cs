using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Utils;
using Serilog;

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

		public async void OnProcessEnd(ProcessDescriptor descriptor)
		{
			try
			{
				if (descriptor.LaunchParameters.CompletionExpiryTime != null)
				{
					if (descriptor.LaunchParameters.CompletionExpiryTime.Value > TimeSpan.Zero)
						await Task.Delay(descriptor.LaunchParameters.CompletionExpiryTime.Value);

					_processes.Remove(descriptor);
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Error while ending task: {Message}", ex.Message);
			}
		}
	}
}
