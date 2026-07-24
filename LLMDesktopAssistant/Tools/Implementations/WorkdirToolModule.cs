using System.Text;
using LLMDesktopAssistant.LLM.Domain;
using Material.Icons;

namespace LLMDesktopAssistant.Tools.Implementations
{
	[ToolModule(chatScoped: true)]
	public class WorkdirToolModule : ToolModule
	{
		private readonly Chat _chat;

		public WorkdirToolModule(Chat chat)
		{
			_chat = chat;

			AddTool(ListWorkingDirectories,
				new ToolInitializationInfo
				{
					Name = "wd-list",
					Description = "Lists all working directories configured for the current chat session.",
					Category = "workdir"
				});

			AddTool(SwitchWorkingDirectory,
				new ToolInitializationInfo
				{
					Name = "wd-switch",
					Description = "Switches the working directory for the current chat session.",
					Category = "workdir",
					DefaultExpectedBehaviour = ToolBehaviour.WorkdirChange,
					SynchronizationGroup = "wd-switch" // Prevent parallel execution of this tool
				});
		}

		private ReactiveToolResult ListWorkingDirectories()
		{
			var sb = new StringBuilder();

			foreach (var wd in _chat.Settings.Environment.WorkingDirectories)
				if (wd.IsEnabled)
					sb.AppendLine($"- *{wd.Name ?? "null"}*: {wd.Path}{(wd.IsActive ? " **(ACTIVE)**" : "")}");

			if (sb.Length == 0)
				sb.AppendLine("No working directories configured.");

			sb.AppendLine().AppendLine("Note: you can directly access files without typing the entire working directory path if it is active.  ");
			sb.Append("Example: `fs-read_entry` with path='.' will read all contents from the active working directory.");

			return new ReactiveToolResult
			{
				StatusIcon = MaterialIconKind.FolderStar,
				ResultContent = sb.ToString(),
				UseMarkdown = true
			}.CompleteWithSuccess();
		}

		private ReactiveToolResult SwitchWorkingDirectory(string name)
		{
			if (!_chat.Settings.Environment.WorkingDirectories.Any(wd => wd.Name == name && wd.IsEnabled))
			{
				return new ReactiveToolResult
				{
					StatusIcon = MaterialIconKind.FolderAlert,
					StatusTitle = $"*{name}*",
					ResultContent = $"Working directory *{name}* not found or it's disabled.",
					UseMarkdown = true
				}.CompleteWithError();
			}

			// Prevent to activate multiple working directories with the same name.
			bool onceFlag = true;
			string? path = null;
			foreach (var wd in _chat.Settings.Environment.WorkingDirectories)
			{
				wd.IsActive = wd.Name == name && onceFlag;
				if (wd.IsActive)
				{
					onceFlag = false;
					path = wd.Path;
				}
			}

			return new ReactiveToolResult
			{
				StatusIcon = MaterialIconKind.FolderArrowRight,
				StatusTitle = $"*{name}*",
				ResultContent = $"Working directory *{name}* ({path}) has been activated.",
				UseMarkdown = true
			}.CompleteWithSuccess();
		}
	}
}
