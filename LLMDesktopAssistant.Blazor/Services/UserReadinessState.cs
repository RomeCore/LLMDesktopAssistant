namespace LLMDesktopAssistant.Blazor.Services
{
	public class UserReadinessState : NotifyPropertyChanged
	{
		public required string Login { get; init; }

		private bool _isReady = false;
		public bool IsReady
		{
			get => _isReady;
			set => SetProperty(ref _isReady, value);
		}

		private bool _isAlwaysReady = false;
		public bool IsAlwaysReady
		{
			get => _isAlwaysReady;
			set => SetProperty(ref _isAlwaysReady, value);
		}
	}
}
