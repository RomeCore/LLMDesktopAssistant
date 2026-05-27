# Preview Executor — Pre-Execution Preview & Danger Assessment

**Status:** Proposal  
**Author:** User request  
**Date:** 2026-05-27

---

## 1. Problem

Currently, when a tool is about to execute, the user sees:

```
[Tool: execute-shell]  arguments: { "command": "rm -rf /" }
[Confirm] [Cancel]
```

The user must either:
- **Blindly confirm** — risky, especially for dangerous operations
- **Stare at the raw JSON arguments** — slow, error-prone, user-unfriendly
- **Open the tool call and manually parse** — painful for complex tools

There is **no intermediate step** that:
- Shows what the tool WILL do in human-readable form
- Assesses the danger level programmatically
- Auto-confirms obviously safe operations (e.g., `ls`, `git status`)
- Warns about destructive side effects (e.g., overwriting files, deleting directories)

---

## 2. Proposed Solution: Preview Executor

A **Preview Executor** is a lightweight, synchronous, read-only pre-check that runs BEFORE the real executor and BEFORE the confirmation dialog. It produces:

- **Status text** — "Will delete file `foo.txt`", "Will search for pattern `bar` in `src/`"
- **Status icon** — MaterialIconKind (e.g., `Delete` for dangerous ops, `FileSearch` for reads)
- **Danger level** — `Safe`, `Warning`, `Danger`, `Critical`
- **Danger reason** — human-readable explanation of why it's dangerous
- **Auto-confirm hint** — whether this tool call can be auto-approved without user interaction

### 2.1 Pipeline Integration

Current flow:
```
ExecuteAsync()
  → IToolExecutionHook (optional short-circuit)
  → AskForConfirmation? (wait for user)
  → Set Executing status
  → tool.Executor.Invoke()
  → ReactiveToolResult
```

New flow:
```
ExecuteAsync()
  → IToolExecutionHook (optional short-circuit)
  → PREVIEW: 
  │   - If tool implements IPreviewExecutor, call it
  │   - Show icon + status + danger level in UI
  │   - Auto-confirm if Safe level
  │   - Color-code (green/yellow/red/dark-red)
  → AskForConfirmation? (with preview visible)
  → Set Executing status
  → tool.Executor.Invoke()
  → ReactiveToolResult
```

---

## 3. Interfaces

### 3.1 IPreviewExecutor

```csharp
/// <summary>
/// Provides a lightweight, read-only preview of what a tool will do
/// BEFORE execution. This is used for:
/// - Showing human-readable status before confirmation
/// - Assessing danger level programmatically
/// - Auto-confirming safe operations
/// - Color-coding dangerous operations
/// </summary>
public interface IPreviewExecutor
{
    /// <summary>
    /// Generates a preview of the tool execution.
    /// This method MUST be:
    /// - Fast (no network calls, no file writes)
    /// - Side-effect free (read-only)
    /// - Synchronous (or very fast async)
    /// </summary>
    ToolPreview? GetPreview(JsonNode args, ToolExecutionContext context);
}

public class ToolPreview
{
    /// <summary>Human-readable status text shown in UI.</summary>
    /// <example>"Will delete file 'temp.txt'"</example>
    public required string StatusText { get; init; }
    
    /// <summary>Icon shown next to the status text.</summary>
    public MaterialIconKind? Icon { get; init; }
    
    /// <summary>Programmatic danger assessment.</summary>
    public DangerLevel Danger { get; init; } = DangerLevel.Unknown;
    
    /// <summary>Human-readable explanation of why this danger level was assigned.</summary>
    public string? DangerReason { get; init; }
    
    /// <summary>
    /// If true, this tool call will be auto-confirmed without user interaction.
    /// Only set to true for trivially safe operations (e.g., reading a file).
    /// </summary>
    public bool AutoConfirm { get; init; } = false;
}

public enum DangerLevel
{
    /// <summary>No preview available (unknown what tool does).</summary>
    Unknown = 0,
    
    /// <summary>Read-only, no side effects. Safe to auto-confirm.</summary>
    Safe = 1,
    
    /// <summary>Reads data but may expose sensitive info. Ask user.</summary>
    Info = 2,
    
    /// <summary>Writes/modifies data. Potential for damage.</summary>
    Warning = 3,
    
    /// <summary>Destructive operations (delete, overwrite, rm -rf).</summary>
    Danger = 4,
    
    /// <summary>Extremely dangerous (format disk, shutdown, sudo without validation).</summary>
    Critical = 5,
}
```

### 3.2 Integration Point

In `ToolExecutionService.ExecuteAsync()`, the preview step is inserted before confirmation:

```csharp
public async Task ExecuteAsync(AssistantMessage message, ToolCall toolCall, 
    LLMInfo llmInfo, ImmutableDictionary<string, ToolInfo> tools, 
    CancellationToken cancellationToken = default)
{
    // ... existing hook check ...
    
    if (!tools.TryGetValue(toolCall.ToolName, out var toolInfo))
    {
        // ... error handling ...
        return;
    }

    try
    {
        // ===== NEW: PREVIEW STEP =====
        var preview = GeneratePreview(toolCall, toolInfo);
        if (preview != null)
        {
            toolCall.Preview = preview;
            
            // Auto-confirm safe operations
            if (preview.AutoConfirm)
            {
                // Skip confirmation, proceed to execution
            }
            else
            {
                // Show preview in UI, wait for user
                toolCall.Status = ToolStatus.WaitingForApproval;
                toolCall.StatusIcon = preview.Icon;
                toolCall.StatusTitle = preview.StatusText;
                // Color is derived from preview.Danger in the UI layer
                
                // ... existing confirmation logic ...
            }
        }
        // ===== END PREVIEW STEP =====
        
        // ... existing execution logic ...
    }
}
```

---

## 4. Built-in Preview Implementations

### 4.1 FilesystemToolModule Previews

```csharp
// Each tool in FilesystemToolModule gets a preview:

// fs-delete_file
AddPreview("fs-delete_file", (args, ctx) => {
    var path = args["path"]?.GetValue<string>();
    return new ToolPreview {
        StatusText = $"Will DELETE file '{path}'",
        Icon = MaterialIconKind.Delete,
        Danger = DangerLevel.Danger,
        DangerReason = "Permanently deletes a file. This cannot be undone.",
        AutoConfirm = false
    };
});

// fs-write_file
AddPreview("fs-write_file", (args, ctx) => {
    var path = args["path"]?.GetValue<string>();
    var exists = File.Exists(ResolvePath(path, ctx));
    return new ToolPreview {
        StatusText = exists 
            ? $"Will OVERWRITE file '{path}'" 
            : $"Will CREATE file '{path}'",
        Icon = exists ? MaterialIconKind.FileReplace : MaterialIconKind.FilePlus,
        Danger = exists ? DangerLevel.Warning : DangerLevel.Safe,
        DangerReason = exists 
            ? "Overwrites existing file. Previous content will be lost." 
            : null,
        AutoConfirm = !exists  // auto-confirm new files, ask for overwrites
    };
});

// fs-rename_file
AddPreview("fs-rename_file", (args, ctx) => {
    var oldPath = args["oldPath"]?.GetValue<string>();
    var newPath = args["newPath"]?.GetValue<string>();
    return new ToolPreview {
        StatusText = $"Will RENAME '{oldPath}' → '{newPath}'",
        Icon = MaterialIconKind.Rename,
        Danger = DangerLevel.Warning,
        DangerReason = "Renames/moves a file. Other references may break.",
        AutoConfirm = false
    };
});

// fs-read_entry (read-only, auto-confirm)
AddPreview("fs-read_entry", (args, ctx) => {
    var path = args["path"]?.GetValue<string>();
    return new ToolPreview {
        StatusText = $"Will READ '{path}'",
        Icon = MaterialIconKind.FileDocumentOutline,
        Danger = DangerLevel.Safe,
        AutoConfirm = true  // reading is always safe
    };
});

// fs-grep
AddPreview("fs-grep", (args, ctx) => {
    var pattern = args["pattern"]?.GetValue<string>();
    var path = args["path"]?.GetValue<string>() ?? ".";
    return new ToolPreview {
        StatusText = $"Will SEARCH for '{pattern.Truncate(50)}' in '{path}'",
        Icon = MaterialIconKind.FileSearchOutline,
        Danger = DangerLevel.Safe,
        AutoConfirm = true
    };
});

// fs-apply_diff
AddPreview("fs-apply_diff", (args, ctx) => {
    var path = args["path"]?.GetValue<string>();
    var hasDelete = args["deleteLines"] != null;
    var hasInsert = args["insertAtLine"] != null;
    var ops = (hasDelete ? "DELETE lines + " : "") + (hasInsert ? "INSERT at line" : "");
    return new ToolPreview {
        StatusText = $"Will APPLY diff to '{path}': {ops}",
        Icon = MaterialIconKind.FileCode,
        Danger = DangerLevel.Warning,
        DangerReason = "Modifies file content. Verify the diff is correct.",
        AutoConfirm = false
    };
});
```

### 4.2 ShellInterpreterToolModule Preview (Danger Assessment)

```csharp
// execute-shell — the most critical one
AddPreview("execute-shell", (args, ctx) => {
    var command = args["command"]?.GetValue<string>() ?? "";
    var danger = AssessShellCommand(command);
    return new ToolPreview {
        StatusText = danger == DangerLevel.Safe 
            ? $"Will RUN: {command.Truncate(80)}"
            : $"Will RUN: {command.Truncate(80)} ⚠️",
        Icon = danger switch {
            DangerLevel.Safe => MaterialIconKind.Console,
            DangerLevel.Warning => MaterialIconKind.Alert,
            DangerLevel.Danger => MaterialIconKind.AlertCircle,
            DangerLevel.Critical => MaterialIconKind.ShieldOff,
            _ => MaterialIconKind.Console
        },
        Danger = danger,
        DangerReason = GetShellDangerReason(command, danger),
        AutoConfirm = danger <= DangerLevel.Safe
    };
});

private static DangerLevel AssessShellCommand(string cmd)
{
    var lower = cmd.Trim().ToLowerInvariant();
    
    // Trivially safe commands — auto-confirm
    if (IsSafeCommand(lower))
        return DangerLevel.Safe;
    
    // Read-only commands that may expose info
    if (IsReadCommand(lower))
        return DangerLevel.Info;
    
    // Write/modify commands
    if (IsWriteCommand(lower))
        return DangerLevel.Warning;
    
    // Destructive commands
    if (IsDestructiveCommand(lower))
        return DangerLevel.Danger;
    
    // Extremely dangerous
    if (IsCriticalCommand(lower))
        return DangerLevel.Critical;
    
    // Unknown — warn
    return DangerLevel.Warning;
}

private static bool IsSafeCommand(string cmd)
{
    // Commands that are trivially safe and should auto-confirm
    return new[] {
        "ls", "echo", "pwd", "whoami", "date", "which", "where", 
        "type", "help", "clear", "cls", "dir", "git status",
        "git branch", "git log", "git diff", "git --version",
        "dotnet --version", "dotnet --list-sdks", "dotnet --list-runtimes",
        "python --version", "node --version", "npm --version",
        "cargo --version", "rustc --version", "rustup show",
    }.Any(safe => cmd.StartsWith(safe) && !cmd.Contains("&&") && !cmd.Contains("|"));
    // Note: simple pipes like `ls | head` are still safe,
    // but complex pipelines with destructive commands in them are not.
}

private static bool IsReadCommand(string cmd)
{
    return new[] {
        "cat", "head", "tail", "less", "more", "find", "grep",
        "git show", "git blame", "git --no-pager", "print",
        "Get-Content", "Select-String", "Get-ChildItem"
    }.Any(read => cmd.StartsWith(read));
}

private static bool IsWriteCommand(string cmd)
{
    return new[] {
        "touch", "mkdir", "cp", "mv", "rename", "chmod", "chown",
        "echo >", "echo >>", "write", "out-file",
        "git add", "git commit", "git push", "git pull",
        "git checkout", "git merge", "git rebase",
        "dotnet build", "npm install", "pip install", "cargo build",
        "Set-Content", "Add-Content", "New-Item"
    }.Any(write => cmd.StartsWith(write));
}

private static bool IsDestructiveCommand(string cmd)
{
    return new[] {
        "rm", "rmdir", "del", "rd /s", "rm -rf",
        "format", "fdisk", "mkfs",
        "git reset --hard", "git clean",
        "drop", "truncate",
        "Remove-Item", "Clear-Content"
    }.Any(destructive => cmd.StartsWith(destructive));
}

private static bool IsCriticalCommand(string cmd)
{
    return new[] {
        "sudo rm -rf /", "sudo dd", "sudo format",
        ":(){ :|:& };:",  // fork bomb (bash)
        "shutdown", "reboot", "halt",
        "init 0", "init 6"
    }.Any(critical => cmd.Contains(critical));
}

private static string? GetShellDangerReason(string cmd, DangerLevel danger)
{
    return danger switch {
        DangerLevel.Safe => null,
        DangerLevel.Info => "This command reads data that may contain sensitive information.",
        DangerLevel.Warning => "This command modifies files or system state.",
        DangerLevel.Destructive => "This command can permanently delete data.",
        DangerLevel.Critical => "EXTREMELY DANGEROUS: This can destroy the operating system.",
        _ => "Unknown command — proceed with caution."
    };
}
```

### 4.3 WebRequestToolModule Preview

```csharp
AddPreview("web-request", (args, ctx) => {
    var method = args["method"]?.GetValue<string>() ?? "GET";
    var url = args["url"]?.GetValue<string>() ?? "";
    
    return new ToolPreview {
        StatusText = $"Will {method} {url.Truncate(100)}",
        Icon = method switch {
            "GET" => MaterialIconKind.Web,
            "POST" => MaterialIconKind.WebPlus,
            "PUT" => MaterialIconKind.WebUpload,
            "DELETE" => MaterialIconKind.WebRemove,
            _ => MaterialIconKind.Web
        },
        Danger = method switch {
            "GET" => DangerLevel.Safe,
            "POST" => DangerLevel.Warning,
            "PUT" => DangerLevel.Warning,
            "DELETE" => DangerLevel.Warning,
            _ => DangerLevel.Info
        },
        DangerReason = method != "GET" 
            ? "This will modify data on the remote server." 
            : null,
        AutoConfirm = method == "GET"
    };
});
```

### 4.4 MetaToolModule Preview

```csharp
AddPreview("metatools-create_or_update", (args, ctx) => {
    var name = args["name"]?.GetValue<string>();
    var lang = args["language"]?.GetValue<string>();
    return new ToolPreview {
        StatusText = $"Will CREATE/UPDATE meta tool '{name}' ({lang})",
        Icon = MaterialIconKind.HammerWrench,
        Danger = DangerLevel.Warning,
        DangerReason = "Creates a new tool that LLM can execute. Verify the code is safe.",
        AutoConfirm = false
    };
});

AddPreview("metatools-delete", (args, ctx) => {
    var name = args["name"]?.GetValue<string>();
    return new ToolPreview {
        StatusText = $"Will DELETE meta tool '{name}'",
        Icon = MaterialIconKind.DeleteForever,
        Danger = DangerLevel.Danger,
        DangerReason = "Permanently removes a custom tool.",
        AutoConfirm = false
    };
});
```

---

## 5. UI Integration

### 5.1 Color Scheme by Danger Level

| Level | Color | Background | Example |
|---|---|---|---|
| `Unknown` | Gray | `#666666` | Tool call with no preview |
| `Safe` | Green | `#27ae60` | `ls`, `read_file` |
| `Info` | Blue | `#2980b9` | `cat config.json` |
| `Warning` | Yellow/Orange | `#f39c12` | `write_file` (overwrite) |
| `Danger` | Red | `#e74c3c` | `rm file.txt` |
| `Critical` | Dark Red | `#c0392b` | `sudo rm -rf /` |

### 5.2 ToolCall Status Enhancement

In `ToolCall.cs`, add preview support:

```csharp
public partial class ToolCall : NotifyPropertyChanged
{
    // ... existing properties ...
    
    private ToolPreview? _preview;
    /// <summary>
    /// Preview information shown BEFORE execution.
    /// Set by PreviewExecutor, read by UI.
    /// </summary>
    public ToolPreview? Preview
    {
        get => _preview;
        set => SetProperty(ref _preview, value);
    }
    
    /// <summary>
    /// Color derived from Preview.Danger for UI binding.
    /// </summary>
    public Color DangerColor => Preview?.Danger switch
    {
        DangerLevel.Safe => Colors.Green,
        DangerLevel.Info => Colors.Blue,
        DangerLevel.Warning => Colors.Orange,
        DangerLevel.Danger => Colors.Red,
        DangerLevel.Critical => Colors.DarkRed,
        _ => Colors.Gray
    };
}
```

### 5.3 View Binding (ToolCallView.axaml)

```xml
<!-- In ToolCallView, before the execution status: -->
<Border 
    IsVisible="{Binding Preview, Converter={x:Static notNullToBool}}"
    Background="{Binding DangerColor, Converter={x:Static toBrush}}"
    CornerRadius="4" Padding="8,4" Margin="0,4">
    <StackPanel Orientation="Horizontal" Spacing="8">
        <PathIcon Icon="{Binding Preview.Icon}" />
        <TextBlock Text="{Binding Preview.StatusText}" />
        <TextBlock Text="{Binding Preview.DangerReason}" 
                   Foreground="Gray" FontSize="11"
                   IsVisible="{Binding Preview.DangerReason, Converter={x:Static notNullToBool}}" />
    </StackPanel>
</Border>
```

---

## 6. Registration of Preview Executors

Previews can be registered alongside tool executors in two ways:

### 6.1 Inline Registration (existing ToolModule)

Extend `ToolInitializationInfo` with an optional preview:

```csharp
public class ToolInitializationInfo
{
    // ... existing properties ...
    
    /// <summary>
    /// Optional preview executor for pre-execution preview.
    /// </summary>
    public Func<JsonNode, ToolExecutionContext, ToolPreview?>? PreviewExecutor { get; init; }
}
```

And `ToolModule.AddTool()`:

```csharp
protected void AddTool(Delegate executor, ToolInitializationInfo info)
{
    var tool = ToolInfo.Create(executor, info);
    if (info.PreviewExecutor != null)
        tool.PreviewExecutor = info.PreviewExecutor;
    _tools.Add(tool);
}
```

### 6.2 Separate Registration (IToolPreviewProvider)

For clean separation, previews can be registered as separate services:

```csharp
public interface IToolPreviewProvider
{
    /// <summary>Tool name this provider handles.</summary>
    string ToolName { get; }
    
    /// <summary>Generate preview for the given arguments.</summary>
    ToolPreview? GetPreview(JsonNode args, ToolExecutionContext context);
}
```

Registered via DI and collected in `ToolExecutionService`.

---

## 7. Implementation Plan

### Phase 1: Infrastructure (1-2 days)

1. Create `ToolPreview` class and `DangerLevel` enum
2. Add `Preview` property to `ToolCall`
3. Add `PreviewExecutor` to `ToolInfo` (optional)
4. Insert preview step in `ToolExecutionService.ExecuteAsync()`

### Phase 2: Previews for critical tools (2-3 days)

5. `ShellInterpreterToolModule` — danger assessment for shell commands
6. `FilesystemToolModule` — previews for all 18 file tools
7. `WebRequestToolModule` — method-based danger assessment
8. `MetaToolModule` — previews for meta-tool management

### Phase 3: UI (1-2 days)

9. Add preview display in `ToolCallView.axaml`
10. Color-code by danger level
11. Bind preview icon + status text

### Phase 4: Rest of tools (2-3 days)

12. `FormsToolModule` — previews for confirm/choice/input/file_picker
13. `RandomToolModule` — previews (trivial)
14. `TimeToolModule` — previews
15. `AgenticToolModule` — previews
16. `WebSearchToolModule` — previews

---

## 8. Examples of Preview in Action

### 8.1 Safe command — auto-confirmed

```
User asks: "List files in current directory"
LLM calls: execute-shell { command: "ls -la" }

Preview: [🖥️] Will RUN: ls -la  (green, auto-confirmed)
→ Execution proceeds without user interaction
Result: total 42, drwxr-xr-x ...
```

### 8.2 Dangerous command — red warning

```
User asks: "Delete the temp file"
LLM calls: fs-delete_file { path: "temp.txt" }

Preview: [🗑️] Will DELETE file 'temp.txt'  (red)
Danger: Permanently deletes a file. This cannot be undone.
→ User must confirm before execution
```

### 8.3 Overwrite warning — yellow caution

```
User asks: "Update the config file"
LLM calls: fs-write_file { path: "config.json", content: "..." }

Preview: [📝] Will OVERWRITE file 'config.json'  (orange)
Danger: Overwrites existing file. Previous content will be lost.
→ User can decide to confirm or cancel

vs. NEW file (green, auto-confirmed):
Preview: [📄] Will CREATE file 'config.json'  (green, auto-confirmed)
```

### 8.4 Shell command danger assessment

```
Command: "rm -rf node_modules && npm install"
Preview: [⚠️] Will RUN: rm -rf node_modules && npm install  (red)
Danger: Contains destructive command 'rm -rf'. Files will be permanently deleted.

Command: "git status"
Preview: [🖥️] Will RUN: git status  (green, auto-confirmed)

Command: "sudo rm -rf / --no-preserve-root"
Preview: [🛡️] Will RUN: sudo rm -rf / --no-preserve-root  (dark red)
Danger: EXTREMELY DANGEROUS: This can destroy the operating system.
```

### 8.5 Web request method-based

```
POST request: web-request { method: "POST", url: "https://api.example.com/data" }
Preview: [🌐+] Will POST https://api.example.com/data  (orange)
Danger: This will modify data on the remote server.

GET request: web-request { method: "GET", url: "https://api.example.com/data" }
Preview: [🌐] Will GET https://api.example.com/data  (green, auto-confirmed)
```

---

---

## 9. Tool Classification & Permission System

The Preview is inherently tied to a permission system. Each tool should declare **what it does** (classification), and the user should be able to configure **auto-approval rules** based on those classifications.

### 9.1 Tool Classification

Every tool gets a `ToolClass` describing its fundamental operation:

```csharp
/// <summary>
/// Classifies what a tool fundamentally does.
/// Used for permission evaluation and preview generation.
/// </summary>
[Flags]
public enum ToolClass
{
    /// <summary>Unknown or uncategorized tool.</summary>
    Unknown = 0,
    
    /// <summary>Reads data without side effects (safe).</summary>
    Read = 1 << 0,
    
    /// <summary>Writes/creates new data.</summary>
    Write = 1 << 1,
    
    /// <summary>Modifies existing data (update, edit, patch).</summary>
    Modify = 1 << 2,
    
    /// <summary>Deletes data permanently.</summary>
    Delete = 1 << 3,
    
    /// <summary>Executes arbitrary code/commands (shell, script).</summary>
    Execute = 1 << 4,
    
    /// <summary>Network operations (HTTP requests, downloads).</summary>
    Network = 1 << 5,
    
    /// <summary>Manages tools/plugins/meta tools (create, delete, rename).</summary>
    ToolManagement = 1 << 6,
    
    /// <summary>Agent management (call other agents, describe images).</summary>
    AgentControl = 1 << 7,
    
    /// <summary>Interacts with user (forms, confirmations).</summary>
    UserInteraction = 1 << 8,
    
    /// <summary>Configuration changes (settings, environment).</summary>
    Configuration = 1 << 9,
    
    /// <summary>Random generation (math, random, time).</summary>
    Generation = 1 << 10,
}
```

### 9.2 Tool Class Mapping for Built-in Tools

```csharp
// FilesystemToolModule
"fs-read_entry"         → Read
"fs-get_file_info"      → Read
"fs-read_binary_file"   → Read
"fs-read_document_file" → Read
"fs-grep"               → Read
"fs-write_file"         → Write
"fs-write_binary_file"  → Write | Modify (overwrite)
"fs-apply_diff"         → Modify
"fs-replace"            → Modify
"fs-copy_file"          → Write
"fs-copy_directory"     → Write
"fs-rename_file"        → Modify
"fs-move_directory"     → Modify
"fs-create_directory"   → Write
"fs-delete_file"        → Delete
"fs-delete_directory"   → Delete
"fs-open_file"          → Execute

// WebRequestToolModule
"web-request" GET       → Read | Network
"web-request" POST/PUT  → Write | Network
"web-request" DELETE    → Delete | Network
"web-download"          → Write | Network
"web-fetch"             → Read | Network
"web-parse"             → Read | Network
"web-status"            → Read | Network

// ShellInterpreterToolModule
"execute-shell"         → Execute
"execute-powershell"    → Execute

// PythonInterpreterToolModule
"execute-python"        → Execute

// MetaToolModule
"metatools-create_or_update" → ToolManagement | Write
"metatools-delete"           → ToolManagement | Delete
"metatools-rename"           → ToolManagement | Modify

// AgenticToolModule
"agent-ask_question"    → AgentControl | Network
"agent-call"            → AgentControl | Execute
"agent-describe_image"  → AgentControl | Read

// FormsToolModule
"forms-confirm"         → UserInteraction
"forms-choice"          → UserInteraction
"forms-input"           → UserInteraction
"forms-file_picker"     → UserInteraction

// MathematicsToolModule
"calculate"             → Generation

// RandomToolModule
"random-*"              → Generation

// TimeToolModule
"time-get"              → Generation
"time-wait"             → Generation

// WebSearchToolModule
"web-search"            → Read | Network
```

### 9.3 User-Configurable Permission Settings

In `AgentToolSettings` (or a new `ToolPermissionsConfiguration`), the user can set auto-approval rules:

```csharp
public class ToolPermissionSettings : SettingsObject
{
    /// <summary>
    /// Tool classes that are automatically approved without user confirmation.
    /// Default: Read, Generation, UserInteraction
    /// </summary>
    public ToolClass AutoApproveClasses { get; set; } = 
        ToolClass.Read | ToolClass.Generation | ToolClass.UserInteraction;
    
    /// <summary>
    /// Tool classes that are ALWAYS denied (blocked) regardless of other settings.
    /// Useful for administrators to restrict dangerous operations.
    /// Default: None
    /// </summary>
    public ToolClass BlockedClasses { get; set; } = ToolClass.Unknown;
    
    /// <summary>
    /// List of specific tool names that are always auto-approved (overrides classification).
    /// </summary>
    public List<string> AlwaysApprovedTools { get; set; } = new();
    
    /// <summary>
    /// List of specific tool names that are always blocked.
    /// </summary>
    public List<string> AlwaysBlockedTools { get; set; } = new();
    
    /// <summary>
    /// Whether to show a warning banner for dangerous tool classes.
    /// </summary>
    public bool ShowDangerWarnings { get; set; } = true;
}
```

### 9.4 Updated Confirmation Logic (OR operator)

The final confirmation decision uses this logic:

```
CanExecute = 
    // Check explicit blocks first
    IsToolAlwaysBlocked(name) ? false :
    
    // Check explicit allows
    IsToolAlwaysApproved(name) ? true :
    
    // Check classification-based blocks
    HasBlockedClass(tool.ToolClass) ? false :
    
    // Classification-based auto-approve (Preview.AutoConfirm AND user setting)
    (Preview.AutoConfirm && UserHasApprovedClass(tool.ToolClass)) ? true :
    
    // Default: ask user
    AskUser();
```

In code:

```csharp
private bool ShouldAutoConfirm(ToolInfo tool, ToolPreview? preview)
{
    var settings = _chat.Settings.Permissions; // ToolPermissionSettings
    
    // 1. Explicitly blocked tools
    if (settings.AlwaysBlockedTools.Contains(tool.Name, StringComparer.OrdinalIgnoreCase))
        return false; // will be denied
    
    // 2. Explicitly approved tools
    if (settings.AlwaysApprovedTools.Contains(tool.Name, StringComparer.OrdinalIgnoreCase))
        return true;
    
    // 3. Blocked classes
    if (settings.BlockedClasses.HasFlag(tool.ToolClass))
        return false; // will be denied
    
    // 4. Classification-based auto-approve (AND logic: preview + user settings)
    if (preview?.AutoConfirm == true && 
        settings.AutoApproveClasses.HasFlag(tool.ToolClass))
        return true;
    
    // 5. Default: ask
    return false;
}

private bool ShouldBlock(ToolInfo tool)
{
    var settings = _chat.Settings.Permissions;
    
    if (settings.AlwaysBlockedTools.Contains(tool.Name, StringComparer.OrdinalIgnoreCase))
        return true;
    
    if (settings.BlockedClasses.HasFlag(tool.ToolClass))
        return true;
    
    return false;
}
```

### 9.5 Integration into ToolExecutionService

```csharp
public async Task ExecuteAsync(...)
{
    // ... existing code ...
    
    try
    {
        // ===== NEW: PREVIEW + PERMISSION CHECK =====
        var preview = GeneratePreview(toolCall, toolInfo);
        if (preview != null)
            toolCall.Preview = preview;
        
        // Check if tool is blocked
        if (ShouldBlock(toolInfo))
        {
            toolCall.Status = ToolStatus.Error;
            toolCall.ResultContent = $"Tool '{toolCall.ToolName}' is blocked by permission settings " +
                $"(class: {toolInfo.ToolClass}). Enable it in settings to use this tool.";
            return;
        }
        
        // Check if should auto-confirm
        bool autoConfirm = ShouldAutoConfirm(toolInfo, preview);
        
        if (toolInfo.AskForConfirmation && !autoConfirm)
        {
            // Show preview + ask user for confirmation
            var tcs = new TaskCompletionSource<string?>(...);
            toolCall.UserConfirmationSource = tcs;
            toolCall.Status = ToolStatus.WaitingForApproval;
            
            // Preview is already visible in the UI at this point
            // User sees: [icon] Will DELETE file 'x' (red) — [Confirm] [Cancel]
            
            string? confirmation = await tcs.Task;
            // ... handle cancellation ...
        }
        // ===== END PREVIEW + PERMISSION CHECK =====
        
        // ... existing execution ...
    }
}
```

### 9.6 UI for Permission Settings

A new settings page where the user can configure:

```
┌──────────────────────────────────────────────────┐
│  Tool Permissions                         Reset  │
├──────────────────────────────────────────────────┤
│                                                  │
│  Auto-Approve by Classification:                 │
│                                                  │
│  ☑ Read — reading files, search, grep           │
│  ☑ Generation — math, random, time              │
│  ☑ User Interaction — forms, confirmations      │
│  ☐ Write — creating new files                   │
│  ☐ Modify — editing existing files              │
│  ☐ Network — web requests, search, download     │
│  ☐ Delete — deleting files and directories      │
│  ☐ Execute — shell commands, scripts             │
│  ☐ Tool Management — meta tools, plugins        │
│  ☐ Agent Control — calling other agents         │
│  ☐ Configuration — changing settings            │
│                                                  │
│  ⚠️  Blocked Classes (deny all):                 │
│  ☐ (none selected)                              │
│                                                  │
│  Always-Approved Tools:                          │
│  [+ Add]                                         │
│  • git-status  (read)                            │
│  • pip-install (write)                           │
│                                                  │
│  Always-Blocked Tools:                           │
│  [+ Add]                                         │
│  • dangerous-script  (execute)                   │
│                                                  │
│  ☑ Show danger warnings in chat                 │
│                                                  │
└──────────────────────────────────────────────────┘
```

### 9.7 Complete Decision Matrix

| Preview.AutoConfirm | User Class Setting | Tool AskForConfirmation | Result |
|---|---|---|---|
| `true` | ✅ Approved | `true` | ✅ **Auto-confirmed** (both agree) |
| `true` | ❌ Not approved | `true` | ❌ **Ask user** (user setting overrides) |
| `true` | ✅ Approved | `false` | ✅ **Auto-confirmed** (no confirmation needed) |
| `false` | ✅ Approved | `true` | ❌ **Ask user** (preview says not safe) |
| `false` | ❌ Not approved | `true` | ❌ **Ask user** (both say no) |
| `null` | — | `true` | ❌ **Ask user** (no preview) |
| `null` | — | `false` | ✅ **Execute directly** (compat) |

### 9.8 Relationship: Preview + Permissions + AskForConfirmation

```
                        ┌──────────────────┐
                        │  Tool Definition │
                        │                  │
                        │  ToolClass: Write │
                        │  AskForConfirm: true│
                        └────────┬─────────┘
                                 │
                    ┌────────────▼────────────┐
                    │   PreviewExecutor        │
                    │                          │
                    │  AutoConfirm = false     │
                    │  (because file exists)   │
                    │  Danger = Warning        │
                    └────────────┬────────────┘
                                 │
                    ┌────────────▼────────────┐
                    │   User Permission Settings│
                    │                          │
                    │  Write: NOT approved     │
                    │  → User must confirm     │
                    └────────────┬────────────┘
                                 │
                    ┌────────────▼────────────┐
                    │   Result: Ask User      │
                    │                          │
                    │  Shows: 🟡 Will OVERWRITE│
                    │  'config.json'           │
                    │  [Confirm] [Cancel]      │
                    └─────────────────────────┘
```

If the user later approves `Write` class in settings:
```
                    ┌────────────▼────────────┐
                    │   User Permission Settings│
                    │                          │
                    │  Write: ✅ APPROVED     │
                    │  + Preview says not safe │
                    │  → Still ask (AND logic) │
                    └─────────────────────────┘
```

The **AND logic** ensures that even if the user broadly approves a class, the Preview still has veto power for specific dangerous operations within that class.

---

## 10. Benefits Summary

| Benefit | Description |
|---|---|
| **Faster workflow** | Safe tools auto-confirm, no unnecessary clicks |
| **Safer execution** | Destructive operations highlighted in red before confirmation |
| **Better UX** | Human-readable status instead of raw JSON |
| **Context-aware warnings** | "Will OVERWRITE" vs "Will CREATE" — user knows before confirming |
| **Shell command classification** | Auto-detect `rm -rf`, `sudo`, fork bombs |
| **Programmatic danger assessment** | Consistent ratings across all tools |
| **Extensible** | Each tool can provide its own preview logic |
| **Backward compatible** | Tools without previews continue working (gray/Unknown) |
