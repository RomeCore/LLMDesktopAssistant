using System.ComponentModel;

namespace LLMDesktopAssistant.Blazor.Services
{
	public interface IGenerationReadinessService : INotifyPropertyChanged
	{
		int ReadyCount { get; }
		int TotalCount { get; }

		UserReadinessState GetReadinessState(string login);
	}
}