namespace LLMDesktopAssistant.LLM.Settings
{
	public class AdditionalParameter : NotifyPropertyChanged
	{
		private bool _enabled = true;
		public bool Enabled
		{
			get => _enabled;
			set => SetProperty(ref _enabled, value);
		}

		private string _parameterName = string.Empty;
		public string ParameterName
		{
			get => _parameterName;
			set => SetProperty(ref _parameterName, value);
		}

		private string _parameterValue = string.Empty;
		/// <summary>
		/// The parameter value in JSON format. For example: "[ \"stop sequence 1\", \"stop sequence 2\" ]".
		/// </summary>
		public string ParameterValue
		{
			get => _parameterValue;
			set => SetProperty(ref _parameterValue, value);
		}
	}
}