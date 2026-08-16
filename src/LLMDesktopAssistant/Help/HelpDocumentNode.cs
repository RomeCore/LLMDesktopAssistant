namespace LLMDesktopAssistant.Help;

/// <summary>
/// Represents a node in the help documentation tree. A node can be a document (with content)
/// or a category (a folder that contains child nodes). A node can be both: for example,
/// <c>chat.md</c> with a <c>chat/</c> folder makes the <c>chat</c> node a document
/// that also has children.
/// </summary>
public class HelpDocumentNode : NotifyPropertyChanged
{
	/// <summary>
	/// Gets the localized title of the node.
	/// </summary>
	public string Title
	{
		get => _title;
		private set => SetProperty(ref _title, value);
	}
	private string _title = string.Empty;

	/// <summary>
	/// Gets the full path of the node without the locale suffix (for example <c>chat/tools</c>).
	/// Category nodes have the path of their folder.
	/// </summary>
	public string NodePath { get; }

	/// <summary>
	/// Gets the relative document path (same as <see cref="NodePath"/>), or <see langword="null"/>
	/// for pure category nodes that have no document of their own.
	/// </summary>
	public string? DocumentPath { get; private set; }

	/// <summary>
	/// Gets the child nodes of this node.
	/// </summary>
	public List<HelpDocumentNode> Children { get; } = [];

	/// <summary>
	/// Gets a value indicating whether this node has a document with content.
	/// </summary>
	public bool HasDocument => DocumentPath is not null;

	internal HelpDocumentNode(string nodePath, string? documentPath)
	{
		NodePath = nodePath;
		DocumentPath = documentPath;
	}

	internal void SetTitle(string title) => Title = title;

	internal void SetDocument() => DocumentPath = NodePath;
}
