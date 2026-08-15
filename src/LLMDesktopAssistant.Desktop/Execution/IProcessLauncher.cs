using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Desktop.Execution
{
	public interface IProcessLauncher
	{
		/// <summary>
		/// Launches a process with the specified parameters.
		/// </summary>
		/// <param name="parameters">The parameters to use for launching the process.</param>
		/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
		/// <returns>A <see cref="ProcessDescriptor"/> representing the launched process.</returns>
		public ProcessDescriptor Launch(ProcessLaunchParameters parameters, CancellationToken cancellationToken = default);
	}
}
