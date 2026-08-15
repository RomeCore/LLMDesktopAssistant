using System;

namespace LLMDesktopAssistant.Desktop.Execution.Terminals
{
    /// <summary>
    /// EventArgs for the ProcessExited event.
    /// </summary>
    public class ProcessExitedEventArgs : EventArgs
    {
        public int ExitCode { get; }

        public ProcessExitedEventArgs(int exitCode)
        {
            ExitCode = exitCode;
        }
    }
}
