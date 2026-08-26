using System.Text.Json.Nodes;
using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.Tools;

namespace LLMDesktopAssistant.Tools.Meta
{
	/// <summary>
	/// Interface for managing tools that can be created by LLM via <see cref="MetaToolModule"/>.
	/// </summary>
	public interface IMetaToolManagementService
	{
		/// <summary>
		/// Creates or updates a tool with the given parameters.
		/// If tool does not exists, all parameters must not be null.
		/// </summary>
		/// <param name="name">The name of the tool.</param>
		/// <param name="isLocal">Indicates whether the tool is local- or app-scoped.</param>
		/// <param name="description">The description of the tool.</param>
		/// <param name="title">The human-readable title of the tool.</param>
		/// <param name="category">The category of the tool.</param>
		/// <param name="enabled">Indicates whether the tool is enabled by default.</param>
		/// <param name="approvalLevel">The level of approval for the tool.</param>
		/// <param name="behaviours">The behaviors of the tool. Describes what tool does.</param>
		/// <param name="argumentSchema">The JSON schema describing the arguments for the tool.</param>
		/// <param name="language">The programming language in which the tool is written.</param>
		/// <param name="executionCode">The code to execute when the tool is called.</param>
		void CreateOrUpdateTool(string name, bool? isLocal, string? description, string? title, string? category, bool? enabled,
			ToolApprovalLevel? approvalLevel, ToolBehaviour? behaviours, JsonObject? argumentSchema, ScriptLanguageType? language, string? executionCode);

		/// <summary>
		/// Lists all meta tools found by the locator together with their diagnostics.
		/// Broken tools are included with a fatal diagnostic.
		/// </summary>
		/// <returns>An array of meta tool infos.</returns>
		MetaToolInfo[] ListTools();

		/// <summary>
		/// Saves the raw file contents of a meta tool with the given name.
		/// </summary>
		/// <param name="name">The name of the tool to save.</param>
		/// <param name="content">The raw file contents to write.</param>
		void SaveToolFile(string name, string content);

		/// <summary>
		/// Renames an existing tool from oldName to newName.
		/// </summary>
		/// <param name="oldName">The current name of the tool.</param>
		/// <param name="newName">The new name for the tool.</param>
		void RenameTool(string oldName, string newName);

		/// <summary>
		/// Deletes a tool with the given name.
		/// </summary>
		/// <param name="name">The name of the tool to delete.</param>
		void DeleteTool(string name);

		/// <summary>
		/// Gets all tools that have been created by LLM.
		/// </summary>
		/// <returns>An array of <see cref="ToolInfo"/> objects representing the tools.</returns>
		ToolInfo[] GetMetaTools();
	}
}