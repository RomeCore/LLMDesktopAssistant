# Tool System

> **Part of dASS (Desktop Assistant) documentation**  
> Covers the architecture, lifecycle, and extensibility of the tool (function-calling) system.

---

## 1. Overview

The tool system is the core mechanism that allows LLM agents to interact with the outside world — filesystem, web, shell, forms, other agents, and more. It is designed around **four architectural layers**:

```
┌──────────────────────────────────────────────────────┐
│                    ToolModule(s)                     │  ← Registered via DI
│  (Filesystem, Web, Forms, Random, Agentic, Meta...)  │
├──────────────────────────────────────────────────────┤
│              ToolsetBuildingService                  │  ← Builds the merged pool
│  (ToolModules + MCP + MetaTools + AdditionalTools)   │
├──────────────────────────────────────────────────────┤
│              ToolExecutionService                    │  ← Executes with HITL pipeline
│   (Preview → Confirmation → Execute → Stream)        │
├──────────────────────────────────────────────────────┤
│          ToolInfo / ReactiveToolResult               │  ← Data models
└──────────────────────────────────────────────────────┘
```

---

## 2. Core Data Models

### 2.1 `ToolInfo`

The central class representing a tool. Created either by `ToolExecutorCreator` (from a C# method) or manually.

```csharp
public class ToolInfo
{
    // Unique name (e.g., "fs-read_entry", "web-search")
    public required string Name { get; init; }

    // Dynamic description — a function, evaluated at runtime
    public required Func<string> DescriptionGetter { get; init; }

    // JSON Schema for arguments — auto-generated or manual
    public required JsonObject ArgumentSchema { get; init; }

    // Optional JSON Schema for structured output
    public JsonObject? OutputSchema { get; init; }

    // The main execution function
    public required Func<JsonNode, ToolExecutionContext, CancellationToken, Task<ReactiveToolResult>> Executor { get; init; }

    // Optional pre-execution check
    public Func<JsonNode, ToolExecutionContext, CancellationToken, Task<PreviewToolExecutionResult>>? PreviewExecutor { get; init; }

    // Display metadata
    public string? DisplayName { get; init; }
    public string Category { get; init; } = "general";
    public ToolSource Source { get; init; } = ToolSource.Native;

    // Safety
    public bool Enabled { get; init; } = true;
    public bool AskForConfirmation { get; init; } = false;
    public ToolDangerLevel DefaultDangerLevel { get; init; }

    // Quick helper to create FunctionTool for API registration
    public FunctionTool Tool => new(Name, DescriptionGetter(), ArgumentSchema, ...);
    public FunctionTool GetExecutableTool(ToolExecutionContext ctx) => ...;
}
```

### 2.2 `ToolInitializationInfo`

Simplified initialization object used in `ToolModule` constructors:

```csharp
public class ToolInitializationInfo
{
    public required string Name { get; init; }
    public string Description { init; }
    public Func<string> DescriptionGetter { get; init; }
    public ToolDangerLevel DefaultDangerLevel { get; init; }
    public JsonObject? OutputSchema { get; init; }
    public string? DisplayName { get; set; }
    public string Category { get; set; } = "general";
    public ToolSource Source { get; set; } = ToolSource.Native;
    public bool Enabled { get; set; } = true;
    public bool AskForConfirmation { get; set; } = false;
}
```

### 2.3 `ToolExecutionContext`

Context passed to every tool execution:

```csharp
public class ToolExecutionContext
{
    public required Chat Chat { get; init; }          // The containing chat
    public required AssistantMessage Message { get; init; } // The assistant message
    public required ToolCall Call { get; init; }      // The specific tool call
    public required ToolInfo Info { get; init; }      // The tool definition
}
```

### 2.4 `ReactiveToolResult`

The streaming, observable result of a tool execution — a key differentiator:

```csharp
public class ReactiveToolResult : NotifyPropertyChanged
{
    // Progress (null = indeterminate)
    public double? Progress { get; set; }
    public double MinProgress, MaxProgress;

    // Dynamic status display
    public MaterialIconKind? StatusIcon { get; set; }
    public string? StatusTitle { get; set; }

    // Streaming content (line-by-line observable collection)
    public RangeObservableCollection<string> ResultContentLines { get; }
    public string ResultContent { get; set; }          // Joined text

    // Markdown rendering support
    public bool UseMarkdown { get; set; }

    // Structured JSON result (for external APIs)
    public JsonNode? StructuredResult { get; set; }

    // Completion signal
    public Task<bool> Completion { get; }              // true = success, false = error

    // Factory methods
    public static ReactiveToolResult CreateSuccess(string content);
    public static ReactiveToolResult CreateError(string content);
    public static ReactiveToolResult Create(bool success, string content);
    public static ReactiveToolResult CreateFromResult(ToolResult result);
}
```

### 2.5 `PreviewToolExecutionResult`

Returned by the optional pre-executor to modify or short-circuit execution:

```csharp
public class PreviewToolExecutionResult
{
    public MaterialIconKind? StatusIcon { get; init; }
    public string? StatusTitle { get; init; }
    public bool? InterruptingSuccess { get; init; }  // If set, tool won't execute
    public string? InterruptingContent { get; init; } // Pre-set result content
    public ToolDangerLevel DangerLevel { get; init; }  // Dynamically adjust danger
}
```

### 2.6 `ToolDangerLevel`

```csharp
public enum ToolDangerLevel
{
    Default,    // No override (uses tool's default)
    Safe,       // Read operations, harmless
    Warning,    // May cause minor disruption
    Dangerous   // Potentially harmful (delete, shell, etc.)
}
```

---

## 3. How Tools Are Registered

### 3.1 Via `[ToolModule]` Attribute

Any class decorated with `[ToolModule]` is automatically discovered via reflection and registered in DI:

```csharp
[ToolModule]
public class WebRequestToolModule : ToolModule
{
    public WebRequestToolModule(Chat chat)
    {
        AddTool(WebRequest, new ToolInitializationInfo
        {
            Name = "web-request",
            Description = "Perform a request to a specified URL and method.",
            Category = "web"
        });
    }
}
```

The method itself is the executor — `ToolExecutorCreator` automatically generates JSON Schema from its parameters.

### 3.2 `ToolExecutorCreator` — Auto Schema Generation

Takes any method and:
1. Scans parameters via `JsonMemberAccessor`
2. Generates JSON Schema from parameter types (`[Description]`, `[Enum]`, `[Required]`, default values)
3. Creates an executor function that: `JsonNode → deserialize → invoke method`

**Supported return types**:
| Return Type | Behavior |
|---|---|
| `ReactiveToolResult` / `Task<ReactiveToolResult>` | Full streaming + progress + status |
| `ToolResult` / `Task<ToolResult>` | Simple success/error result |
| `string` / `Task<string>` | Plain text |
| `void` / `Task` | Nothing |

**Special parameter injection**:
| Parameter / Attribute | Source |
|---|---|
| `ToolExecutionContext` | Current execution context |
| `[OriginalArgs] JsonNode` | Raw JSON arguments as received |
| `CancellationToken` | Cancellation token |
| `ReactiveToolResult` | Pre-created result for streaming |
| `[Inject] TService` | DI service from chat context |

### 3.3 Manual Creation

Tools can also be created manually via `ToolInfo` constructor and added via `AddTool()`.

---

## 4. Execution Lifecycle

`ToolExecutionService` implements a full pipeline:

```
LLM calls tool (with JSON arguments)
    │
    ▼
❶ PreviewExecutor (if defined)
   • Can dynamically set DangerLevel based on arguments
   • Can short-circuit execution (InterruptingSuccess + InterruptingContent)
   • Can set status icon/title before execution starts
    │
    ▼
❷ Confirmation Check
   • If AskForConfirmation = true AND DangerLevel > agent's AutoApproveLevel
   • Shows UI form (FormsConfirmViewModel)
   • User can confirm, cancel (with reason), or ignore
    │
    ▼
❸ Argument Parsing
   • Uses TolerantJsonParser — can fix malformed JSON from LLM
   • Falls back gracefully with error message
    │
    ▼
❹ Executor Invocation
   • Calls toolInfo.Executor(parsedArgs, context, cancellationToken)
   • Returns ReactiveToolResult
   • Subscribes to PropertyChanged for streaming icon/title
   • Subscribes to CollectionChanged for streaming content lines
    │
    ▼
❺ Wait for Completion
   • await reactiveResult.Completion → true (success) or false (error)
   • If CancellationToken fires → ToolStatus.Cancelled
    │
    ▼
❻ Error Handling
   • Cancelled → ToolStatus.Cancelled
   • Timeout → ToolStatus.Error
   • Exception → ToolStatus.Error with message
```

---

## 5. Tool Categories

### 5.1 Native Modules (Core Library)

| Module | Tools | Category |
|---|---|---|
| **FilesystemToolModule** (6 sub-modules) | `fs-read_entry`, `fs-write_file`, `fs-edit`, `fs-apply_diff`, `fs-grep`, `fs-glob`, `fs-get_file_info`, `fs-delete_file`, `fs-copy_file`, `fs-rename_file`, `fs-create_directory`, `fs-delete_directory`, `fs-copy_directory`, `fs-move_directory`, `fs-read_binary_file`, `fs-write_binary_file`, `fs-read_document_file` | `filesystem` |
| **WebRequestToolModule** | `web-request`, `web-download`, `web-status`, `web-fetch`, `web-parse` | `web` |
| **WebSearchToolModule** | `web-search` (80+ search engines) | `web` |
| **AgenticToolModule** | `agent-ask_question`, `agent-call`, `agent-describe_image` | `agents` |
| **FormsToolModule** | `forms-confirm`, `forms-choice`, `forms-input`, `forms-file_picker` | `forms` |
| **RandomToolModule** | `random-coin_flip`, `random-check_chance`, `generate_password`, 5 more | `random` |
| **TimeToolModule** | `time-get`, `time-wait` | `time` |
| **MathematicsToolModule** | `calculate` (complex numbers, integrals) | `math` |
| **MetaToolModule** | `metatools-create_or_update`, `metatools-list`, `metatools-get_info`, `metatools-rename`, `metatools-delete` | `metatools` |
| **GeneralToolModule** | Base utilities | `general` |

### 5.2 Desktop Modules

| Module | Tools | Category |
|---|---|---|
| **ShellInterpreterToolModule** | `execute-shell`, `execute-powershell` | `scripting` |
| **PythonInterpreterToolModule** | `execute-python`, `execute-python_venv_shell`, `python-get_installed_packages_list` | `Python` |
| **DesktopFilesystemToolModule** | Desktop-specific file operations | `filesystem` |

### 5.3 MCP Tools

Any MCP server connected via `MCPToolModule`:
- Automatically converts `McpClientTool` → `ToolInfo`
- Tools get server name prefix (e.g., `my-server-fetch`)
- Supports dynamic `tools/list_changed` notifications
- Executor calls the MCP server and maps results

### 5.4 Meta Tools (Runtime-Created)

Tools created by the LLM during runtime, written in **Lua** or **Python**:
- YAML frontmatter (title, description, argument schema, danger level, confirmation)
- Execution code invokes the scripting engine
- Full CRUD via `MetaToolModule`

```python
"""
title: Weather Checker
description: Gets current weather for a location
category: utilities
ask_for_confirmation: false
argument_schema: {"type": "object", "properties": {"location": {"type": "string"}}, "required": ["location"]}
"""
import python_weather
import asyncio

async def getweather():
    async with python_weather.Client() as client:
        location = tool_args["location"]
        weather = await client.get(location)
        print(f"Current temperature: {weather.temperature}°C")

asyncio.run(getweather())
```

---

## 6. Toolset Building for Agents

`ToolsetBuildingService.BuildTools(agentId)` assembles the toolset for a specific agent:

1. **Gather all sources**:
   - All `ToolModule` instances from DI
   - `AdditionalTools` from the chat
   - MCP tools from connected servers
   - Meta tools from `IMetaToolManagementService`

2. **Deduplicate** by tool name (last wins)

3. **Apply agent-specific overrides** (`ToolChanges`):
   - Enable/disable individual tools
   - Override `AskForConfirmation` per tool
   - Filter by `AutoApproveLevel`

---

## 7. Human-in-the-Loop (Forms)

Forms are tools that **pause execution** and show a UI dialog to the user:

| Tool | Purpose |
|---|---|
| `forms-confirm` | Yes/No confirmation (with danger styling) |
| `forms-choice` | Select from options (single/multiple, allow custom) |
| `forms-input` | Structured data input (text, number, password, multiline) |
| `forms-file_picker` | File selection dialog (open/save/directory, filter by extension) |

Each form has its own View/ViewModel pair, registered in DI. The tool awaits the user's response via `TaskCompletionSource`, then resumes execution.

---

## 8. Safety Model

### 8.1 Danger Levels
Every tool has a base `DangerLevel`. Preview executors can **dynamically** adjust it based on runtime arguments:
- `fs-read_entry` → Safe
- `fs-write_file` → Warning
- `fs-delete_file` → Dangerous (or Safe if deleting temp files)

### 8.2 Auto-Approval
Each agent has `AutoApproveLevel` in `AgentToolSettings`:
```csharp
AgentToolSettings.AutoApproveLevel  // ToolDangerLevel
```
If the tool's danger level ≤ agent's auto-approve level → **no confirmation needed**.
Otherwise → **user must confirm**.

### 8.3 Confirmation UI
When confirmation is required:
1. Tool status changes to `ToolStatus.WaitingForApproval`
2. User sees a dialog with the tool's name, description, and arguments
3. User can approve, reject (with reason), or ignore
4. If rejected → tool returns cancellation message to LLM

---

## 9. MCP Integration

The `MCPToolModule` bridges external MCP servers seamlessly:

```csharp
// Auto-converts MCP tool to dASS tool
McpClientTool → ToolInfo with:
  • Name: "{server-name}-{tool-name}"
  • Description: from MCP
  • Arguments: from JSON Schema
  • Executor: calls MCP server, maps result
```

Features:
- Dynamic discovery via `tools/list_changed`
- Automatic tool name sanitization (invalid characters removed)
- Structured results from MCP return content
- Error handling for failed MCP calls

---

## 10. Extensibility Points

| Extension Point | What It Does |
|---|---|
| **Create a `[ToolModule]` class** | Add new native C# tools with auto-generated schemas |
| **Connect an MCP server** | Instantly get all its tools with no code |
| **Create a Meta Tool** | Write Lua/Python tools at runtime from chat |
| **Implement `IMetaToolEngine`** | Add support for new scripting languages |
| **Use `ToolPreviewExecutor`** | Add pre-execution validation and dynamic danger levels |

---

## 11. Best Practices

1. **Always set appropriate `DangerLevel`** — be conservative for destructive operations
2. **Use `PreviewExecutor` for argument validation** — prevents execution of invalid commands
3. **Stream results via `ReactiveToolResult`** — gives users live feedback
4. **Add `[Description]` attributes** to parameters — improves LLM understanding
5. **Use `[Inject]` for needed services** — keeps tool methods clean
6. **Handle `CancellationToken`** — supports cancellation of long-running tools
7. **Set `Category`** — helps LLM organize and discover tools

---

## 12. Architecture Diagram (Text)

```
                    ┌──────────────┐
                    │    LLM       │
                    └──────┬───────┘
                           │ tool_call(tool_name, args)
                           ▼
              ┌────────────────────────┐
              │  ToolExecutionService  │
              │  (per-chat service)    │
              └──────┬──────────┬──────┘
                     │          │
                  Preview?   Execute?
                     │          │
                     ▼          ▼
          ┌───────────────┐  ┌──────────────────┐
          │PreviewExecutor│  │  tool.Executor() │
          └──────┬────────┘  └────────┬─────────┘
                 │                    │
                 ▼                    ▼
        ┌────────────────────────────────┐
        │      ReactiveToolResult        │
        │  ┌─────────────────────────┐   │
        │  │  ResultContentLines     │   │ ← streaming
        │  │  Progress / StatusIcon  │   │ ← real-time UI
        │  │  Completion Task        │   │ ← async await
        │  └─────────────────────────┘   │
        └────────────────────────────────┘
```
