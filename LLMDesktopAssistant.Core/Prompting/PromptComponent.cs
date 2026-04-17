namespace LLMDesktopAssistant.Core.Prompting
{
	public class PromptComponent : NotifyPropertyChanged
	{
		private Guid _id = Guid.NewGuid();
		/// <summary>
		/// Unique identifier for this prompt component.
		/// </summary>
		public Guid Id
		{
			get => _id;
			set => SetProperty(ref _id, value);
		}

		private string _name = string.Empty;
		public string Name
		{
			get => _name;
			set => SetProperty(ref _name, value);
		}

		private string _text = string.Empty;
		public string Text
		{
			get => _text;
			set => SetProperty(ref _text, value);
		}
	}
}
