namespace LLMDesktopAssistant.Tools;

/// <summary>
/// Defines specific behaviours that a tool can exhibit during execution.
/// These flags describe what the tool actually does, enabling fine-grained
/// control over tool approval, security policies, and UI presentation.
/// </summary>
/// <remarks>
/// Use this enum instead of the abstract <see cref="ToolDangerLevel"/> to
/// provide concrete, actionable information about tool capabilities.
/// Each flag represents a distinct category of filesystem, network, or
/// system interaction that may require user awareness or consent.
/// </remarks>
[Flags]
public enum ToolBehaviour
{
	/// <summary>
	/// No specific behaviour flags. The tool performs purely computational
	/// or informational tasks (e.g., math evaluation, random number generation).
	/// </summary>
	None = 0,

	// ────────────────────────────── Filesystem ──────────────────────────────

	/// <summary>
	/// The tool creates new files or directories on the filesystem.
	/// Examples: <c>fs-write_file</c>, <c>fs-copy_file</c>, <c>fs-create_directory</c>.
	/// </summary>
	FileDirectoryCreate = 1 << 0,

	/// <summary>
	/// The tool reads the contents of one or more files or directories.
	/// Examples: <c>fs-read_entry</c>, <c>fs-grep</c>.
	/// </summary>
	FileRead = 1 << 1,

	/// <summary>
	/// The tool modifies existing file content or metadata without deleting it.
	/// Examples: <c>fs-edit</c>, <c>fs-apply_diff</c>, <c>fs-rename_file</c>.
	/// </summary>
	FileEdit = 1 << 2,

	/// <summary>
	/// The tool permanently removes files or directories from the filesystem.
	/// Examples: <c>fs-delete_file</c>, <c>fs-delete_directory</c>.
	/// </summary>
	FileDelete = 1 << 3,

	/// <summary>
	/// The tool enumerates or lists the contents of directories.
	/// Examples: <c>fs-read_entry</c> when listing a directory, <c>fs-glob</c>.
	/// </summary>
	DirectoryRead = 1 << 4,

	/// <summary>
	/// The tool modifies the directory structure (move, rename, copy directories).
	/// Examples: <c>fs-move_directory</c>, <c>fs-copy_directory</c>.
	/// </summary>
	DirectoryEdit = 1 << 5,

	/// <summary>
	/// The tool removes entire directory trees including their contents.
	/// Examples: <c>fs-delete_directory</c>.
	/// </summary>
	DirectoryDelete = 1 << 6,

	// ────────────────────────────── Security ────────────────────────────────

	/// <summary>
	/// The tool may read or expose sensitive information such as passwords,
	/// API keys, tokens, or other secrets from files or environment.
	/// </summary>
	ReadSecrets = 1 << 7,

	/// <summary>
	/// The tool accesses locations outside the configured working directory
	/// (e.g., system paths, user home, temporary folders, environment variables).
	/// This may indicate potential sandbox escape or unintended data exposure.
	/// </summary>
	AccessOutsideWorkdir = 1 << 8,

	/// <summary>
	/// The tool writes text data to the clipboard.
	/// Examples: <c>clipboard-copy</c>.
	/// </summary>
	ClipboardWrite = 1 << 9,

	/// <summary>
	/// The tool reads text data from the clipboard.
	/// This may involve reading sensitive data (passwords, tokens, etc.).
	/// Examples: <c>clipboard-read</c>.
	/// </summary>
	ClipboardRead = 1 << 10,

	// ────────────────────────────── Network ─────────────────────────────────

	/// <summary>
	/// The tool performs network requests to external services or APIs.
	/// Includes HTTP requests, web searches, file downloads, and webhook calls.
	/// Examples: <c>web-fetch</c>, <c>web-search</c>, <c>web-download</c>.
	/// </summary>
	InternetAccess = 1 << 11,

	// ────────────────────────────── Performance ─────────────────────────────

	/// <summary>
	/// The tool may run for an extended period (seconds to minutes) due to
	/// heavy computation, large file processing, deep directory traversal,
	/// or network latency. Examples: <c>fs-grep</c> on large codebase,
	/// large file downloads, AI model inference (image description).
	/// </summary>
	LongRunningTask = 1 << 12,

	// ────────────────────────────── Execution ───────────────────────────────

	/// <summary>
	/// The tool spawns or executes an external operating system process.
	/// This includes running shell commands, scripts, or binaries.
	/// Examples: <c>execute-shell</c>, <c>execute-python</c>.
	/// </summary>
	ExecuteExternalProcess = 1 << 13,

	/// <summary>
	/// The tool may perform actions that are potentially unexpected or uncontrollable,
	/// such as executing scripts that is not analyzed yet.
	/// </summary>
	PossiblyUnexpected = 1 << 14,

	/// <summary>
	/// The tool runs commands in an embedded terminal emulator with
	/// interactive I/O capabilities. This implies direct user interaction
	/// and potential for arbitrary command execution.
	/// Examples: <c>execute-shell</c> with <c>runTerminal: true</c>.
	/// </summary>
	RunTerminal = 1 << 15,

	// ────────────────────────────── User Interaction ────────────────────────

	/// <summary>
	/// The tool requires special user input, such as prompts for confirmation, various forms,
	/// file uploads, or custom UI elements. Examples: <c>forms-input</c>, <c>forms-submit</c>.
	/// </summary>
	UserInteraction = 1 << 16,

	// ────────────────────────────── Agents ──────────────────────────────────

	/// <summary>
	/// The tool invokes, manages, or coordinates other AI agents.
	/// This can trigger cascading agent execution and should be
	/// carefully monitored to prevent runaway agent loops.
	/// Examples: <c>agent-describe_image</c>, agent delegation calls.
	/// </summary>
	AgentExecution = 1 << 17,

	// ────────────────────────────── Source ─────────────────────────────────

	/// <summary>
	/// The tool originates from an external MCP (Model Context Protocol) server.
	/// These tools are provided by external MCP servers and may have
	/// unpredictable behaviour. Treat MCP tools with caution as they
	/// may access external systems or resources outside this application's control.
	/// </summary>
	MCP = 1 << 18,

	/// <summary>
	/// The tool is a meta-tool created at runtime by the LLM itself
	/// (via Lua/Python scripting). Such tools can have arbitrary behaviour
	/// defined by the LLM and should be carefully monitored.
	/// </summary>
	Meta = 1 << 19,


	// ────────────────────────────── Meta ────────────────────────────────────

	/// <summary>
	/// The tool creates, modifies, or deletes other tools and scripts at runtime.
	/// This includes meta-tools, Lua/Python user script registration,
	/// and dynamic tool definitions. Potentially dangerous because
	/// it can alter the assistant's capabilities on the fly.
	/// Examples: <c>metatools-create_or_update</c>, <c>metatools-delete</c>, <c>lua-register_or_update_script</c>.
	/// </summary>
	ScriptAccess = 1 << 20,
}
