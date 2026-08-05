using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Agents.Memory
{
	[SettingsObject("memory_blocks")]
	public class MemoryBlock : SettingsObject
	{
		/// <summary>
		/// Unique identifier for this memory block. This should be unique across all memory blocks in the system.
		/// </summary>
		public Guid DataId { get; set; } = Guid.NewGuid();

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
		/// This will be used for filling, consolidating, and updating the memory block.
		/// </summary>
		public string MaintainerModel
		{
			get => _maintainerModel;
			set => SetProperty(ref _maintainerModel, value);
		}

		/// <summary>
		/// Returns the directory where this memory block's data is stored.
		/// </summary>
		public string GetDataDirectory() => Path.Combine(Directories.Memory, DataId.ToString());
	}
}
