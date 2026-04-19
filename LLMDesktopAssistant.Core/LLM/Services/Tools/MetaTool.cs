using System.Text.Json.Nodes;

namespace LLMDesktopAssistant.LLM.Services.Tools
{
	public class MetaTool : NotifyPropertyChanged
	{
		private string _name = string.Empty;
		public string Name
		{
			get => _name;
			set => SetProperty(ref _name, value);
		}

		private string _description = string.Empty;
		public string Description
		{
			get => _description;
			set => SetProperty(ref _description, value);
		}

		private string _title = string.Empty;
		public string Title
		{
			get => _title;
			set => SetProperty(ref _title, value);
		}

		private string _category = string.Empty;
		public string Category
		{
			get => _category;
			set => SetProperty(ref _category, value);
		}

		private bool _askForConfirmation = false;
		public bool AskForConfirmation
		{
			get => _askForConfirmation;
			set => SetProperty(ref _askForConfirmation, value);
		}

		private JsonObject _argumentSchema = new();
		public JsonObject ArgumentSchema
		{
			get => _argumentSchema;
			set => SetProperty(ref _argumentSchema, value);
		}

		private string _pythonExecutionCode = string.Empty;
		public string PythonExecutionCode
		{
			get => _pythonExecutionCode;
			set => SetProperty(ref _pythonExecutionCode, value);
		}
	}
}