# Agent Dispatcher — Automatic Lifecycle & HITL for Non-Chat Agents, Scheduled Scripts

**Status:** Proposal  
**Author:** Architectural Analysis  
**Date:** 2026-07-09

---

## 1. Motivation

dASS has multiple **non-chat agent** execution paths that currently run invisibly:

| Execution Path | Where | Example |
|----------------|-------|---------|
| **Agentic tools** | `AgenticToolModule` | `agent-ask_question`, `agent-call`, `agent-describe_image` |
| **Lua agent API** | `LuaApiAgents.cs` | `dass.agents.execute()` |
| **Chat naming** | `ChatNamingService` | Auto-naming conversations |
| **Chat summarization** | `ChatSummarizationService` | Background summarization |

All of these:
- 🔴 Have **no UI visibility** — user doesn't know they're running
- 🔴 **Cannot be cancelled** — once started, runs to completion
- 🔴 **No HITL** — tool calls inside these agents execute without approval
- 🔴 **No concurrency control** — multiple agents can stack up uncontrollably
- 🔴 **No scheduling** — can't run Lua scripts on a timer

Additionally, users want to **schedule Lua scripts** (not agents, but arbitrary automation code) to run periodically — e.g. "find a lunch recipe every day at 13:00".

---

## 2. Core Concept

A single **Agent Dispatcher** sits transparently between all non-chat execution sources and the actual runtime:

```
                   ┌──────────────────────┐
                   │   Chat (main loop)    │
                   │   (not dispatched)    │
                   └──────────────────────┘

  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐
  │AgenticTool   │  │Lua           │  │ChatNaming    │  │ChatSummariz. │
  │Module        │  │dass.agents   │  │Service       │  │Service       │
  │(ask, call,   │  │.execute()    │  │              │  │              │
  │ describe_img)│  │              │  │              │  │              │
  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘
         │                 │                 │                 │
         ▼                 ▼                 ▼                 ▼
  ┌─────────────────────────────────────────────────────────────────┐
  │                    Agent Dispatcher                              │
  │  ┌─────────────┐  ┌─────────────┐  ┌────────────────────────┐  │
  │  │ Queue       │  │ Scheduler   │  │ HITL + Approval        │  │
  │  │ (priority)  │  │ (cron)      │  │ (ToolBehaviour-based)  │  │
  │  └─────────────┘  └─────────────┘  └────────────────────────┘  │
  │  ┌─────────────────────────────────────────────────────────┐   │
  │  │ State Machine: Queued → Running → [ToolPending] → Done  │   │
  │  │                                  ↘ Failed / Cancelled   │   │
  │  └─────────────────────────────────────────────────────────┘   │
  └──────────────────────────┬──────────────────────────────────────┘
                             │
                             ▼
                    ┌────────────────┐
                    │  LLM / Tools   │
                    │  Execution     │
                    └────────────────┘
```

**Key principles:**
- **Transparent** — existing code calls agents exactly as before, dispatcher intercepts automatically
- **Zero-config** — all non-chat agents are tracked by default
- **HITL by policy** — tool calls inside agents follow configurable approval rules (auto/confirm/deny)
- **Scheduling for scripts** — cron-style execution of Lua scripts, not agents

---

## 3. Architecture

### 3.1 AgentExecutionEngine — Central Dispatcher

```csharp
[ChatService]
public class AgentExecutionEngine
{
    // Called automatically by all agent execution paths
    Task<AgentExecution> ExecuteAsync(
        AgentExecutionRequest request,
        AgentExecutionContext context
    );

    // Scheduling
    Task<ScheduledScript> CreateScheduleAsync(ScheduledScriptConfig config);
    Task UpdateScheduleAsync(ScheduledScript script);
    Task DeleteScheduleAsync(Guid scheduleId);
    Task EnableAsync(Guid scheduleId);
    Task DisableAsync(Guid scheduleId);

    // Lifecycle control
    Task CancelAsync(Guid executionId);
    Task PauseAsync(Guid executionId);
    Task ResumeAsync(Guid executionId);

    // Query
    Task<AgentExecution?> GetExecutionAsync(Guid executionId);
    IObservable<AgentExecution> Executions { get; }
    IReadOnlyList<AgentExecution> ActiveExecutions { get; }
    IReadOnlyList<ScheduledScript> Schedules { get; }

    // HITL
    IObservable<ToolApprovalRequest> ToolApprovalRequests { get; }
    Task ApproveToolAsync(Guid requestId, JsonNode? modifiedArgs = null);
    Task RejectToolAsync(Guid requestId, string? reason = null);
}
```

**AgentExecution** — state record:

```csharp
public record AgentExecution
{
    Guid Id;
    string Name;
    ExecutionSource Source;
    // AgenticToolModule, LuaAgentApi, ChatNamingService,
    // ChatSummarizationService, ScheduledScript, UserScript
    AgentExecutionStatus Status;
    // Queued, Running, Paused, ToolPending, Completed, Failed, Cancelled
    DateTime CreatedAt;
    DateTime? StartedAt;
    DateTime? CompletedAt;
    string? OwnerInfo;          // e.g. "agent-describe_image", "lunch_script.lua"
    AgentExecutionPolicy Policy;
    IReadOnlyList<ToolCallRecord> ToolCalls;
    IReadOnlyList<string> Log;
    double? Progress;
    string? ErrorMessage;
}
```

**State machine:**

```
                    ┌───▶ Paused ───┐
                    │               │
Queued ──▶ Running ─┼───▶ Completed │
                    ├───▶ Failed     │
                    └───▶ Cancelled  │
                         (any state) │
                              ▲──────┘
                    ToolPending (HITL)
```

### 3.2 Integration Points — Transparent Interception

The dispatcher intercepts via existing infrastructure:

#### 3.2.1 AgenticToolModule

The `AgenticToolModule` registers tools that internally call LLM. Instead of direct execution, they route through `AgentExecutionEngine`:

```csharp
// Before: direct LLM call
public async Task<ToolResult> CallAgentAsync(...)
{
    var response = await executor.GenerateResponseAsync(...);
    return new ToolResult(Success, response.Content);
}

// After: dispatched
public async Task<ToolResult> CallAgentAsync(...)
{
    var execution = await _dispatcher.ExecuteAsync(new()
    {
        Name = "agent-call",
        Source = ExecutionSource.AgenticToolModule,
        Policy = AgentExecutionPolicy.FromGlobal(), // inherits global HITL policy
        ExecuteAsync = async (ctx, ct) =>
        {
            var response = await executor.GenerateResponseAsync(...);
            return response.Content;
        }
    });
    return new ToolResult(execution.Status == Completed ? Success : Error, execution.Result ?? execution.ErrorMessage);
}
```

#### 3.2.2 Lua API (`dass.agents.execute`)

The existing `LuaApiAgents.cs` routes through the dispatcher:

```csharp
// LuaApiAgents.cs already has access to services
public async Task<LuaTuple> ExecuteAsync(LuaTable properties)
{
    var execution = await _dispatcher.ExecuteAsync(new()
    {
        Name = "Lua Agent",
        Source = ExecutionSource.LuaAgentApi,
        ExecuteAsync = async (ctx, ct) =>
        {
            // ... existing agent execution logic ...
            return result;
        }
    });
    return LuaValueConverter.ToLuaValue(execution.Result);
}
```

#### 3.2.3 ChatNamingService & ChatSummarizationService

```csharp
// ChatNamingService
public async Task<bool> NameChatAsync()
{
    var execution = await _dispatcher.ExecuteAsync(new()
    {
        Name = "Chat Naming",
        Source = ExecutionSource.ChatNamingService,
        ExecuteAsync = async (ctx, ct) =>
        {
            // ... existing naming logic ...
            return chatName;
        }
    });
    return execution.Status == Completed;
}
```

#### 3.2.4 Lua Script Execution (scheduled or manual)

When a script is run (whether scheduled or triggered), it goes through the same dispatcher:

```csharp
// LuaService — script execution
public async Task<LuaTuple> ExecuteAsync(string code, ...)
{
    // Check if this execution should be tracked
    if (_dispatcher.IsTrackingEnabled)
    {
        var execution = await _dispatcher.ExecuteAsync(new()
        {
            Name = "Script",
            Source = ExecutionSource.UserScript,
            ExecuteAsync = async (ctx, ct) =>
            {
                return await ExecuteInternalAsync(code, ...);
            }
        });
        return execution.Result;
    }
    return await ExecuteInternalAsync(code, ...);
}
```

### 3.3 AgentQueue — Prioritised Concurrency Control

```csharp
public class AgentQueue
{
    int MaxConcurrent { get; set; } = 3; // configurable

    Task EnqueueAsync(AgentExecutionRequest request, int priority = 0);
    // priority: 0=normal, -1=low, +1=high (e.g. naming/summarization = low)

    int QueuedCount { get; }
    int RunningCount { get; }
    void Clear();
}
```

Default priorities by source:
| Source | Priority |
|--------|----------|
| `agent-ask_question` (user-visible) | +1 (high) |
| `agent-describe_image` (user-visible) | +1 (high) |
| `agent-call` | 0 (normal) |
| Lua `agent.dispatch()` | 0 (normal) |
| `ChatNamingService` | -1 (low) |
| `ChatSummarizationService` | -1 (low) |
| `ScheduledScript` | 0 (normal) |

### 3.4 AgentScheduler — Cron for Lua Scripts

```csharp
public class AgentScheduler
{
    Task<ScheduledScript> CreateAsync(ScheduledScriptConfig config);
    Task UpdateAsync(ScheduledScript script);
    Task DeleteAsync(Guid id);
    Task EnableAsync(Guid id);
    Task DisableAsync(Guid id);

    IReadOnlyList<ScheduledScript> GetAll();
    ScheduledScript? Get(Guid id);
}
```

**ScheduledScript** — persistent entity:

```csharp
public record ScheduledScript
{
    Guid Id;
    string Name;                       // "Обеденный рецепт"
    string? Description;
    string LuaCode;                    // or path to .lua file
    CronExpression Cron;               // "0 13 * * *"
    string TimeZone;                   // "Europe/Moscow"
    DateTime? LastRunAt;
    DateTime? NextRunAt;
    bool IsEnabled;
    AgentExecutionPolicy Policy;       // HITL defaults for this script schedule
    int MaxRetries;
    TimeSpan? Timeout;                 // e.g. 5 minutes
}
```

**CronExpression** supports:
- Standard cron: `0 13 * * *`
- Extended: `*/30 * * * *`
- Human-friendly presets: "every day", "weekdays", "every hour", "custom"

### 3.5 AgentToolApprovalService — HITL

```csharp
public class AgentToolApprovalService
{
    Task<ToolApprovalResult> RequestApprovalAsync(
        AgentExecution execution,
        ToolInfo tool,
        JsonNode args,
        CancellationToken ct
    );
}
```

Uses existing `ToolBehaviour` flags and `ToolApprovalLevel`:

**Policy resolution (inheritance chain):**

```
Global Default Policy
  └── ExecutionSource Policy (e.g. AgenticToolModule default)
       └── Per-execution Policy (passed at dispatch time)
            └── Schedule-specific Policy (for scheduled scripts)
```

**Policy structure:**

```csharp
public record AgentExecutionPolicy
{
    // Explicit lists
    ImmutableList<string> AutoApproveTools; // auto-approve these tools
    ImmutableList<string> ConfirmTools;     // show dialog for these
    ImmutableList<string> DenyTools;        // reject immediately

    // Or use behaviour-based presets
    AgentExecutionPolicyPreset Preset;       // Trusted, Balanced, Strict

    TimeSpan HITLTimeout;                   // default 5 min
    bool AllowArgsModification;             // can user edit args before approving?
}
```

**Presets:**

| Preset | Behaviour |
|--------|-----------|
| `Trusted` | All tools auto-approved |
| `Balanced` | Reads auto-approved; writes confirmed; shell/delete denied |
| `Strict` | All non-computational tools require confirmation |
| `Custom` | Manual lists |

**HITL flow:**

```
Agent calls fs-write_file("recipe.txt", "...")
  → Dispatcher pauses execution (state: ToolPending)
  → UI shows dialog:
      ┌─────────────────────────────────────────┐
      │ 🤖 Agent "Lunch Script" wants to:       │
      │                                          │
      │ 📝 fs-write_file("recipe.txt", "...")    │
      │ 🏷️ Behaviour: FileCreate                 │
      │ ⚠️ Policy: Balanced — requires confirm   │
      │                                          │
      │ [✅ Approve] [✏️ Edit args] [❌ Reject]  │
      │ [⏱️ Auto-deny in 4:59]                   │
      └─────────────────────────────────────────┘
  → User approves → agent resumes
  → User rejects → agent gets ToolResult with error
  → Timeout → auto-deny, agent continues with error
```

---

## 4. UI Components

### 4.1 Agent Dashboard

The main panel showing all tracked executions:

```
┌──────────────────────────────────────────────────────────────┐
│ ⚡ Agent Dispatcher                              [⚙️ Settings]│
├──────────────────────────────────────────────────────────────┤
│ ACTIVE (3)                                                    │
├──────────────────────────────────────────────────────────────┤
│ 🟢 agent-describe_image   ⏳ 45% [████░░░░]  [⏸️] [⏹️]      │
│    ├ ⏱️ 2.3s elapsed                                         │
│    └ 🛑 Pending: none                                        │
├──────────────────────────────────────────────────────────────┤
│ 🟡 Chat Naming            1.2s elapsed         [⏹️]          │
│    └ ✅ Running                                             │
├──────────────────────────────────────────────────────────────┤
│ 🟤 Lunch Script (scheduled) Queued              [✕]         │
├──────────────────────────────────────────────────────────────┤
│ QUEUED (1)                                                    │
├──────────────────────────────────────────────────────────────┤
│ ⏳ Chat Summarization     Priority: low                      │
├──────────────────────────────────────────────────────────────┤
│ RECENT (completed in last 5 min)                             │
├──────────────────────────────────────────────────────────────┤
│ ✅ agent-ask_question     1m ago   4 tool calls     [📋]    │
│ ✅ agent-call             3m ago   HITL: 1 approved [📋]    │
│ ❌ Lua Script             4m ago   Timeout          [📋]    │
├──────────────────────────────────────────────────────────────┤
│ SCHEDULED SCRIPTS (3)                       [➕ Add]        │
├──────────────────────────────────────────────────────────────┤
│ 🔄 13:00 daily    Рецепт обеда        🟢         [✏️] [🔴] │
│ 🔄 09:00 weekdays Утренний дайджест   🟢         [✏️] [🔴] │
│ 🔄 18:00 daily    Бэкап проекта       🔴         [✏️] [🟢] │
└──────────────────────────────────────────────────────────────┘
```

### 4.2 Agent Detail View

Clicking any execution opens details:

```
┌──────────────────────────────────────────────────────────────┐
│ ← Dashboard    🔬 agent-describe_image                       │
├──────────────────────────────────────────────────────────────┤
│ Status: 🟢 Running     Started: 12:59:45   Elapsed: 2.3s    │
│ Source: AgenticToolModule  (triggered by user message)      │
│ Policy: Balanced 🟡                                         │
│ [⏸️ Pause] [⏹️ Stop]                                        │
├──────────────────────────────────────────────────────────────┤
│ TOOL CALL HISTORY                                            │
│ The agent itself is an LLM call — no sub-tools yet.         │
│ LLM: openrouter$google/gemini-3.5-flash                     │
│ Tokens used: 146 in / 32 out                                │
├──────────────────────────────────────────────────────────────┤
│ EXECUTION LOG                                                │
│ [12:59:45] INFO  Agent dispatched from AgenticToolModule    │
│ [12:59:45] INFO  Using vision model: gemini-3.5-flash      │
│ [12:59:45] INFO  Loading image: photo.jpg                   │
│ [12:59:46] INFO  Sending request to LLM...                  │
│ [12:59:47] INFO  Receiving response (streaming)...          │
└──────────────────────────────────────────────────────────────┘
```

### 4.3 Schedule Manager View

```
┌──────────────────────────────────────────────────────────────┐
│ ⏰ Scheduled Scripts                         [➕ Add] [✕]   │
├──────────────────────────────────────────────────────────────┤
│ 🔄 Every day at 13:00     Europe/Moscow                     │
│    📝 Name: Рецепт обеда                                     │
│    📄 Script: user_scripts/lunch_script.lua                  │
│    🛡️ Policy: Balanced (confirm writes)                     │
│    ⏱️ Last: 2026-07-08 13:00 ✅ | Next: 2026-07-09 13:00    │
│    [✏️ Edit] [▶️ Run now] [🔴 Disable] [🗑️ Delete]          │
├──────────────────────────────────────────────────────────────┤
│ 🔄 Mon-Fri at 09:00     Europe/Moscow                       │
│    📝 Name: Утренний дайджест                                │
│    📄 Script: user_scripts/news_digest.lua                   │
│    🛡️ Policy: Trusted (auto-approve all)                    │
│    ⏱️ Last: 2026-07-09 09:00 ✅ | Next: 2026-07-10 09:00    │
│    [✏️ Edit] [▶️ Run now] [🔴 Disable] [🗑️ Delete]          │
└──────────────────────────────────────────────────────────────┘
```

### 4.4 Create/Edit Scheduled Script Dialog

```
┌──────────────────────────────────────────────────────────────┐
│ ✏️ Create Scheduled Script                  [Cancel] [Save] │
├──────────────────────────────────────────────────────────────┤
│ Name:         [Поиск рецепта обеда                         ] │
│ Description:  [Ищет вкусный рецепт и сохраняет в файл     ] │
│ Script:       [📁 user_scripts/lunch_script.lua            ] │
│               [🆕 Create new in editor...                   ] │
│ Schedule:     [🔄 Every day              ] at [13:00] [  ] │
│ Timezone:     [Europe/Moscow            ▼]                  │
│ Max retries:  [2]    Timeout: [5] minutes                   │
├──────────────────────────────────────────────────────────────┤
│ 🛡️ HITL Policy                                              │
│ ┌────────────────────────────────────────────────────────┐  │
│ │ 🟢 Trusted  🟡 Balanced  🔒 Strict  ✏️ Custom          │  │
│ │                                                        │  │
│ │ Auto-Approve: web-search, web-fetch                    │  │
│ │ Confirm:      fs-write_file, fs-edit                   │  │
│ │ Deny:         execute-shell, fs-delete_file            │  │
│ └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
```

---

## 5. Data Storage

Using existing **LiteDB** infrastructure:

```csharp
public class AgentDatabase
{
    ILiteCollection<AgentExecution> Executions;
    ILiteCollection<ScheduledScript> Schedules;
}
```

**Retention policy:**
- Keep last 1000 executions
- Auto-cleanup completed executions older than 30 days
- Keep failed executions for 90 days
- Scheduled scripts kept indefinitely

---

## 6. Existing Infrastructure Reuse

| Component | Reuses | Notes |
|-----------|--------|-------|
| **Lua runtime** | `LuaService`, `LuaState` | Snapshot isolation per execution |
| **Tool system** | `ToolBehaviour`, `ToolInfo`, `ToolModule` | 18 behaviour flags, approval levels |
| **Security** | `ToolApprovalLevel`, `PreviewExecutor` | HITL confirmation dialogs |
| **UI patterns** | Existing Avalonia dialogs, toasts | Same approval UX as chat |
| **Logging** | `dass.log`, `Serilog` | Structured logging |
| **Storage** | `LiteDB`, `ChatDatabase` pattern | `AgentDatabase` |
| **DI** | `ServiceRegistry`, `[ChatService]` | Registration |
| **AgenticToolModule** | Existing 3 agent tools | Intercept at executor level |
| **ChatNamingService** | Existing naming logic | Wrap in dispatch |
| **ChatSummarizationService** | Existing summarization | Wrap in dispatch |
| **LuaApiAgents** | Existing `LuaApiAgents.cs` | Route through dispatcher |

---

## 7. Implementation Roadmap

### Phase 1 — Core Engine + UI Dashboard (MVP)
- `AgentExecutionEngine` with state machine (Queued → Running → Completed/Failed/Cancelled)
- `AgentQueue` with priority
- UI: Dashboard with active/queued/recent executions
- Transparent interception of `AgenticToolModule` agents
- Cancel/stop from UI

### Phase 2 — HITL
- `AgentToolApprovalService`
- Tool call interception and suspension
- Approval dialogs in UI (reuse chat pattern)
- Policy presets (Trusted/Balanced/Strict)
- Integration with Lua `dass.agents.execute()`

### Phase 3 — Scheduling
- `AgentScheduler` with cron
- LiteDB persistence for schedules
- UI: Schedule manager view
- "Run now" button
- Integration with Lua script execution

### Phase 4 — Complete Coverage
- Intercept `ChatNamingService` and `ChatSummarizationService`
- Execution history browser with filtering
- Notifications (toast on completion/failure)
- Export/import schedules
- Per-source default policies
- Script editor with syntax highlighting in create dialog

---

## 8. Open Questions

1. **Should chat naming/summarization be cancellable?**
   - They are fast (<5s usually). Proposal: track them but don't show in dashboard by default (filterable).

2. **HITL timeout behaviour?**
   - Proposal: configurable per policy (default 5 min). On timeout → deny, agent gets error result, continues.

3. **What about agents launched from within agents (nested)?**
   - Proposal: flat list in dashboard, but with parent-child relationship shown in detail view.

4. **Concurrency limits per source?**
   - Proposal: global max (default 3) + optional per-source limits (e.g. max 1 summarization at a time).

5. **Persistence of running state across app restart?**
   - Proposal: mark all running agents as "Interrupted" on graceful shutdown. Non-graceful → "Failed".

6. **Resource limits for scripts?**
   - Proposal: `max_execution_time` per schedule (default 5 min). Memory limits are hard in Lua — defer.

7. **Should scheduled scripts survive app close?**
   - Yes — schedules are persisted. Missed runs: execute on next startup if `run_on_startup_if_missed` is set.

---

## 9. Appendix: Example Lua Scripts for Scheduling

### Example 1: Daily Lunch Recipe Hunter

```lua
-- lunch_script.lua
-- Scheduled: every day at 13:00
local day = datetime.now_local()
local dayOfWeek = day.day_of_week
local cuisine = {"italian", "asian", "mexican", "russian", "mediterranean"}
local pick = cuisine[dayOfWeek % #cuisine + 1]

local results = await web.search("easy " .. pick .. " lunch recipe")
if #results.results == 0 then
    dass.toasts.warning("No recipes found", "Try different cuisine tomorrow")
    return
end

local recipe = await web.fetch(results.results[1].url)
fs.write("today_lunch.md", "# " .. pick:upper() .. " Lunch\n\n" .. recipe)
dass.toasts.success("Recipe saved!", "Check today_lunch.md for " .. pick .. " recipe")
```

### Example 2: Morning News Digest

```lua
-- news_digest.lua
-- Scheduled: weekdays at 09:00
local categories = {"tech", "science", "world"}
local digest = {}

for _, cat in ipairs(categories) do
    local news = await web.search(cat .. " news today", { maxResults = 5 })
    digest[cat] = news.results
end

local dateStr = datetime.format(datetime.now_local(), "dd.MM.yyyy")
local md = "# Morning Digest " .. dateStr .. "\n\n"
for cat, articles in pairs(digest) do
    md ..= "## " .. cat:upper() .. "\n"
    for _, a in ipairs(articles) do
        md ..= "- [" .. a.title .. "](" .. a.url .. ")\n"
    end
    md ..= "\n"
end
fs.write("morning_digest.md", md)
dass.toasts.info("Morning digest ready!", dateStr)
```

### Example 3: Weekly Project Backup

```lua
-- backup_script.lua
-- Scheduled: every Sunday at 22:00
local dateStr = datetime.format(datetime.now_local(), "yyyy-MM-dd")
local backupDir = "backups/" .. dateStr

fs.create_dir(backupDir)
local files = fs.glob("**/*.{cs,lua,md,json,csproj}")
for _, file in ipairs(files) do
    fs.copy(file, backupDir .. "/" .. file)
end

dass.toasts.success("Backup completed", dateStr .. " — " .. #files .. " files saved")
```

### Example 4: On-Demand Code Inspector (manual trigger via UI)

```lua
-- inspect_script.lua
-- Can be triggered manually from UI "Run now"
local todos = {}
local files = fs.glob("**/*.cs")
for _, file in ipairs(files) do
    local content = fs.read(file)
    for line in content:gmatch("[^\n]+") do
        if line:find("TODO") or line:find("FIXME") or line:find("HACK") then
            table.insert(todos, string.format("- %s: `%s`", file, line:trim()))
        end
    end
end

if #todos > 0 then
    fs.write("todo_report.md", "# TODO/FIXME/HACK Report\n\n" .. table.concat(todos, "\n"))
    dass.toasts.warning("Found " .. #todos .. " issues", "Check todo_report.md")
else
    dass.toasts.success("Clean code!", "No TODO/FIXME/HACK found")
end
```
