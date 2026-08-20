using LLMDesktopAssistant.LLM.Settings;

namespace LLMDesktopAssistant.LLM.Services
{
	public interface IWorkingDirectoryAccessService
	{
		string AccessPath(string path, DirectoryAccessMode mode);
		string CheckedAccessPath(string path, DirectoryAccessMode mode, out bool isAccessed);
		string GetWorkingDirectory();
		string? TryAccessPath(string path, DirectoryAccessMode mode);
	}
}