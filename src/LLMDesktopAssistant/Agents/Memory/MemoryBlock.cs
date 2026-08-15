using LLMDesktopAssistant.Settings;

namespace LLMDesktopAssistant.Agents.Memory
{
	[SettingsObject("memory_blocks")]
	public class MemoryBlock : SettingsObject
	{
		private string _name = string.Empty;
		/// <summary>
		/// Gets or sets the (display) name of the memory block. This is used to identify the block within the system.
		/// </summary>
		public string Name
		{
			get => _name;
			set => SetProperty(ref _name, value);
		}

		private string _description = string.Empty;
		/// <summary>
		/// Gets or sets the description of the memory block.
		/// This is used to provide additional context about the block's purpose or content for both agents and users.
		/// </summary>
		public string Description
		{
			get => _description;
			set => SetProperty(ref _description, value);
		}

		private string _embeddingModel = string.Empty;
		/// <summary>
		/// Gets or sets the embedding model associated with this memory block.
		/// This will be used for creating embeddings when indexing the inner vector database.
		/// </summary>
		public string EmbeddingModel
		{
			get => _embeddingModel;
			set => SetProperty(ref _embeddingModel, value);
		}

		private string _maintainerModel = string.Empty;
		/// <summary>
		/// Gets or sets the model used for maintaining this memory block.
		/// This will be used for consolidating the memory block.
		/// </summary>
		public string MaintainerModel
		{
			get => _maintainerModel;
			set => SetProperty(ref _maintainerModel, value);
		}

		private bool _factsEnabled = true;
		/// <summary>
		/// Gets or sets a value indicating whether facts are enabled for this memory block.
		/// </summary>
		public bool FactsEnabled
		{
			get => _factsEnabled;
			set => SetProperty(ref _factsEnabled, value);
		}

		private bool _logsEnabled = false;
		/// <summary>
		/// Gets or sets a value indicating whether memory logs are enabled for this memory block.
		/// </summary>
		public bool LogsEnabled
		{
			get => _logsEnabled;
			set => SetProperty(ref _logsEnabled, value);
		}

	}
}
