using System.Text;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Utils;
using Material.Icons;
using LLMDesktopAssistant.Localization;

namespace LLMDesktopAssistant.Tools.Implementations
{
	[ToolModule(chatScoped: true)]
	public class WorkdirToolModule : ToolModule
	{
		private readonly IChatSettingsService _chatSettings;

		public WorkdirToolModule(IChatSettingsService chatSettings)
		{
			_chatSettings = chatSettings;

			AddTool(new ToolInitializationInfo
			{
				Executor = ListWorkingDirectories,
				Name = "wd-list",
				Description = "Lists all working directories configured for the current chat session.",
				TitleKey = Locale.GetKey("tool.name.wd-list"),
				DescriptionKey = Locale.GetKey("tool.description.wd-list"),
				CategoryKey = Locale.GetKey("tool.category.workdir")
			});

			AddTool(new ToolInitializationInfo
			{
				Executor = SwitchWorkingDirectory,
				PreviewExecutor = SwitchWorkingDirectoryPreview,
				Name = "wd-switch",
				Description = "Switches the working directory for the current chat session.",
				TitleKey = Locale.GetKey("tool.name.wd-switch"),
				DescriptionKey = Locale.GetKey("tool.description.wd-switch"),
				CategoryKey = Locale.GetKey("tool.category.workdir"),
				DefaultExpectedBehaviour = ToolBehaviour.WorkdirChange,
				SynchronizationGroup = "wd-switch" // Prevent parallel execution of this tool
			});
		}

		private ReactiveToolResult ListWorkingDirectories()
		{
			var sb = new StringBuilder();

			var workingDirectories = _chatSettings.Settings.Environment.GetEffectiveWorkingDirectories();

			if (workingDirectories.IsDefaultWorkingDirectoryEnabled)
				sb.AppendLine($"- *{Directories.DefaultWorkingDirectoryName}*: {Directories.DefaultWorkingDirectory}{(workingDirectories.IsDefaultWorkingDirectoryActive ? " **(ACTIVE)**" : "")}");

			foreach (var wd in workingDirectories.Items)
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

		private PreviewToolExecutionResult SwitchWorkingDirectoryPreview(string name)
		{
			var workingDirectories = _chatSettings.Settings.Environment.GetEffectiveWorkingDirectories();
			if (name == Directories.DefaultWorkingDirectoryName && workingDirectories.IsDefaultWorkingDirectoryEnabled)
			{
				return new PreviewToolExecutionResult
				{
					StatusIcon = MaterialIconKind.FolderArrowRight,
					StatusTitle = $"*{name}*"
				};
			}

			if (!workingDirectories.Items.Any(wd => wd.Name == name && wd.IsEnabled))
			{
				return new PreviewToolExecutionResult
				{
					StatusIcon = MaterialIconKind.FolderAlert,
					StatusTitle = $"*{name}*",
					InterruptingSuccess = false,
					InterruptingContent = $"Working directory *{name}* not found or it's disabled.",
					UseMarkdown = true
				};
			}

			return new PreviewToolExecutionResult
			{
				StatusIcon = MaterialIconKind.FolderArrowRight,
				StatusTitle = $"*{name}*"
			};
		}

		private ReactiveToolResult SwitchWorkingDirectory(string name)
		{
			var workingDirectories = _chatSettings.Settings.Environment.GetEffectiveWorkingDirectories();
			if (name == Directories.DefaultWorkingDirectoryName && workingDirectories.IsDefaultWorkingDirectoryEnabled)
			{
				workingDirectories.IsDefaultWorkingDirectoryActive = true;
				foreach (var wd in workingDirectories.Items)
					wd.IsActive = false;

				return new ReactiveToolResult
				{
					StatusIcon = MaterialIconKind.FolderAlert,
					StatusTitle = $"*{name}*",
					ResultContent = $"Working directory *{name}* not found or it's disabled.",
					UseMarkdown = true
				}.CompleteWithSuccess();
			}

			if (!workingDirectories.Items.Any(wd => wd.Name == name && wd.IsEnabled))
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
			foreach (var wd in workingDirectories.Items)
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
