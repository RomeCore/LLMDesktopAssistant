# Worker System — Fire-and-Forget Background Agent Execution

**Status:** Proposal  
**Author:** Architectural Analysis  
**Date:** 2026-05-26

---

## 1. Motivation

dASS is fundamentally **chat-centric** — a sequential conversation where messages are ordered, branching is used for memory/alternatives, and only one agent executes at a time. This is perfect for interactive use cases with multiple users and agents.

However, there are scenarios where **fire-and-forget** background execution is valuable:

- Long-running code refactoring
- Background research / web scraping
- Parallel task execution (multiple independent searches)
- CI/CD integration (run tests, linter, formatter in background)
- Scheduled/recurring tasks (nightly dependency updates)

Claw Code solves this with a full-blown **Worker State Machine** (`WorkerCreate` → `WorkerObserve` → `WorkerResolveTrust` → `WorkerSendPrompt` → `WorkerObserveCompletion`). We can achieve the same with our existing architecture, where **each worker is simply a separate Chat session** running in the background.

---

## 2. Architecture — Chat as Worker

### 2.1 Core Concept

In dASS, the **Chat is the First Class Citizen**. A worker is nothing more than:

```
Worker = Chat + Agent + Background Task
```

Each worker:
- Has its own `Chat` instance (with its own messages, settings, agents)
- Runs in a background `Task`
- Reports status back to the parent chat
- Can be observed, cancelled, and restarted

### 2.2 Interfaces

```csharp
/// <summary>
/// Service for managing background worker agents.
/// </summary>
public interface IWorkerService
{
    /// <summary>Create a new worker and start it immediately.</summary>
    Task<WorkerInfo> CreateWorkerAsync(CreateWorkerRequest request, CancellationToken ct = default);
    
    /// <summary>Get worker status and result.</summary>
    Task<WorkerInfo?> GetWorkerAsync(string workerId, CancellationToken ct = default);
    
    /// <summary>List all workers.</summary>
    Task<IReadOnlyList<WorkerInfo>> ListWorkersAsync(CancellationToken ct = default);
    
    /// <summary>Cancel and terminate a running worker.</summary>
    Task<bool> StopWorkerAsync(string workerId, CancellationToken ct = default);
    
    /// <summary>Wait for worker to complete (with timeout).</summary>
    Task<WorkerInfo> AwaitWorkerAsync(string workerId, TimeSpan? timeout = null, CancellationToken ct = default);
}

public record CreateWorkerRequest
{
    /// <summary>Agent ID to execute.</summary>
    public required Guid AgentId { get; init; }
    
    /// <summary>The prompt/message to send to the agent.</summary>
    public required string Prompt { get; init; }
    
    /// <summary>Optional: override allowed tools for this worker.</summary>
    public string[]? AllowedTools { get; init; }
    
    /// <summary>Optional: working directory.</summary>
    public string? WorkingDirectory { get; init; }
    
    /// <summary>Optional: parent chat ID for result delivery.</summary>
    public int? ParentChatId { get; init; }
    
    /// <summary>Whether to auto-recover on prompt delivery failure.</summary>
    public bool AutoRecover { get; init; } = false;
}

public record WorkerInfo
{
    public string WorkerId { get; init; }
    public WorkerStatus Status { get; init; }
    public Guid AgentId { get; init; }
    public string Prompt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? ResultSummary { get; init; }
    public string? ErrorMessage { get; init; }
    public int ChatId { get; init; }  // The internal chat session
    public int Attempts { get; init; }
}

public enum WorkerStatus
{
    Spawning,
    Running,
    Completed,
    Failed,
    Cancelled
}
```

### 2.3 Implementation Sketch

```csharp
[ChatService(typeof(IWorkerService))]
public class WorkerService : IWorkerService
{
    private readonly ConcurrentDictionary<string, WorkerHandle> _workers = new();
    private readonly IServiceProvider _services;
    
    public async Task<WorkerInfo> CreateWorkerAsync(CreateWorkerRequest request, CancellationToken ct)
    {
        // 1. Create a new Chat for this worker
        var workerChat = new Chat(CreateWorkerServiceProvider());
        workerChat.Settings = GetParentChat()?.Settings.Clone() ?? DefaultSettings();
        
        // 2. Create worker info
        var workerId = $"worker_{Guid.NewGuid():N}";
        var info = new WorkerInfo
        {
            WorkerId = workerId,
            Status = WorkerStatus.Spawning,
            AgentId = request.AgentId,
            Prompt = request.Prompt,
            CreatedAt = DateTime.UtcNow,
            ChatId = workerChat.ChatId
        };
        
        // 3. Start background execution
        var cts = new CancellationTokenSource();
        var handle = new WorkerHandle(workerChat, cts);
        _workers[workerId] = handle;
        
        _ = Task.Run(async () =>
        {
            try
            {
                // Send user message to worker chat
                var storage = workerChat.Services.GetRequiredService<IChatStorageService>();
                storage.AppendMessage(new UserMessage { Content = request.Prompt });
                
                // Execute agent
                var executor = workerChat.Services.GetRequiredService<IChatExecutionService>();
                await executor.GenerateResponseWithAgentAsync(request.AgentId, 
                    GetDefaultStageId(request.AgentId), cts.Token);
                
                // Collect result
                var lastMsg = workerChat.Messages.LastOrDefault()?.Message as AssistantMessage;
                UpdateWorkerInfo(workerId, WorkerStatus.Completed, 
                    resultSummary: lastMsg?.Content);
            }
            catch (OperationCanceledException)
            {
                UpdateWorkerInfo(workerId, WorkerStatus.Cancelled);
            }
            catch (Exception ex)
            {
                UpdateWorkerInfo(workerId, WorkerStatus.Failed, errorMessage: ex.Message);
            }
        }, CancellationToken.None);
        
        return info;
    }
    
    // ... other methods
}
```

---

## 3. Worker Lifecycle State Machine

```
                    ┌─────────────┐
                    │  Spawning   │
                    └──────┬──────┘
                           │
                           ▼
                    ┌─────────────┐
                    │   Running   │
                    └──────┬─────┘
                           │
              ┌────────────┼────────────┐
              ▼            ▼            ▼
       ┌──────────┐ ┌──────────┐ ┌──────────┐
       │Completed │ │ Failed   │ │Cancelled │
       └──────────┘ └──────────┘ └──────────┘
```

### 3.1 Worker Events (for observability)

```csharp
public record WorkerEvent
{
    public string WorkerId { get; init; }
    public int Seq { get; init; }
    public WorkerEventKind Kind { get; init; }
    public string? Detail { get; init; }
    public DateTime Timestamp { get; init; }
}

public enum WorkerEventKind
{
    Created,
    Started,
    AgentResponse,
    ToolCall,
    ToolResult,
    Completed,
    Failed,
    Cancelled,
    Heartbeat
}
```

---

## 4. Tool Module for Worker Management

Using the **Meta Tool** system or a dedicated `ToolModule`, we expose worker management as regular tools:

```csharp
[ToolModule]
public class WorkerToolModule : ToolModule
{
    public WorkerToolModule(IWorkerService workerService)
    {
        AddTool(CreateWorker, new ToolInitializationInfo
        {
            Name = "worker-create",
            Description = """
                Creates a background worker agent that executes a task independently.
                The worker runs in its own chat session and reports back when done.
                Use this for long-running or parallel tasks.
                
                Parameters:
                - agent_name: string — name of the agent to execute
                - prompt: string — the task description
                - tools: string[] (optional) — allowed tool names
                - auto_recover: bool (optional) — auto-recover on failure
                
                Returns worker_id which can be used with worker-get, worker-await, etc.
                """,
            Category = "workers",
            AskForConfirmation = true
        });

        AddTool(GetWorker, new ToolInitializationInfo
        {
            Name = "worker-get",
            Description = "Get the status and result of a background worker by ID.",
            Category = "workers"
        });

        AddTool(ListWorkers, new ToolInitializationInfo
        {
            Name = "worker-list",
            Description = "List all background workers and their status.",
            Category = "workers"
        });

        AddTool(AwaitWorker, new ToolInitializationInfo
        {
            Name = "worker-await",
            Description = "Wait for a worker to complete and return its result.",
            Category = "workers"
        });

        AddTool(StopWorker, new ToolInitializationInfo
        {
            Name = "worker-stop",
            Description = "Stop/cancel a running background worker.",
            Category = "workers",
            AskForConfirmation = true
        });

        AddTool(CreateTaskPacket, new ToolInitializationInfo
        {
            Name = "worker-task_packet",
            Description = """
                Creates a structured task packet with objective, scope, acceptance tests,
                and escalation policy. The packet is executed by a background worker.
                """,
            Category = "workers",
            AskForConfirmation = true
        });
    }
}
```

---

## 5. Integration with Existing System

### 5.1 Using Meta Tools (Lua)

Since we already have Lua scripting, workers can be created directly from Lua:

```lua
-- Create a background worker via Lua
local worker = dass.tools.call("worker-create", {
    agent_name = "Coder",
    prompt = "Refactor the database layer to use async/await",
    tools = {"bash", "read_file", "write_file", "edit_file", "grep_search"}
})

print("Worker ID: " .. worker.content)

-- ... do other work ...

-- Await the worker result
local result = dass.tools.call("worker-await", {
    worker_id = worker.content
})
print("Worker result: " .. result.content)
```

### 5.2 Using Agentic Lua API

Since `dass.agents.execute()` already exists, we can wrap it for fire-and-forget:

```lua
-- Fire-and-forget via a meta tool wrapper
local function fire_and_forget(prompt, tools)
    -- This creates a background task via worker-create
    return dass.tools.call("worker-create", {
        agent_name = "DefaultAgent",
        prompt = prompt,
        tools = tools or {"bash", "read_file", "write_file"}
    })
end

-- Launch parallel tasks
local w1 = fire_and_forget("Search for latest Rust async patterns")
local w2 = fire_and_forget("Read the database migration guide")

-- Check results later
local r1 = dass.tools.call("worker-await", { worker_id = w1.content })
local r2 = dass.tools.call("worker-await", { worker_id = w2.content })
```

---

## 6. Comparison with Claw Code Workers

| Feature | Claw Code | dASS (Proposed) |
|---|---|---|
| **State machine** | 7 states: Spawning → TrustRequired → ToolPermissionRequired → ReadyForPrompt → Running → Finished/Failed | 5 states: Spawning → Running → Completed/Failed/Cancelled |
| **Worker ID format** | `worker_{timestamp}_{counter}` | `worker_{guid}` |
| **Trust gate** | Auto-resolve via trusted_roots + manual | Not needed (agents are pre-configured) |
| **Tool permission gate** | Detects permission prompts in terminal output | Not needed (tools are pre-configured per agent) |
| **Prompt misdelivery detection** | Scans terminal output for shell vs agent mismatch | Not applicable (chat-based, not terminal) |
| **Auto-recovery** | `auto_recover_prompt_misdelivery` + replay_prompt | Can re-queue on failure |
| **Event system** | WorkerEvent with seq, kind, payload | WorkerEvent with seq, kind, detail |
| **Startup timeout** | `StartupEvidenceBundle` + failure classification | Timeout via `AwaitWorkerAsync(timeout)` |
| **Background tasks** | `TaskCreate/Get/List/Stop/Update/Output` | `WorkerCreate/Get/List/Await/Stop` |
| **Cron / scheduled** | `CronCreate/Delete/List` | Can be added later via `ITeamCronService` |
| **Teams** | `TeamCreate/Delete` — parallel multi-agent | Can be added later |
| **Chat integration** | No (CLI-only, terminal-based) | **Native** — workers are chats with full UI |
| **Result delivery** | Must poll via TaskGet/TaskOutput | Can push result back to parent chat |

### 6.1 Advantages of dASS's Approach

1. **No trust/permission gates needed** — agents in dASS have pre-configured permissions and tool sets. The complex trust detection Claw Code needs (scanning terminal output for `>>> Allow?`) simply doesn't apply.

2. **Full UI for workers** — since workers are Chats, they automatically get:
   - Message history with Markdown rendering
   - Tool call visualization with progress
   - Branching for exploring alternatives
   - Token counting and usage stats

3. **Result can push to parent chat** — worker can append its result as a message in the main chat when done.

4. **Existing infrastructure** — `ChatExecutionService`, `ToolExecutionService`, `ChatStorageService` all work without modification.

---

## 7. Simplified State Machine (No Trust/Permission Gates)

Unlike Claw Code's workers that run in raw terminals and must detect trust prompts, tool permission prompts, and shell misdelivery — dASS workers run inside the existing chat infrastructure where:

- **Agent capabilities are known at creation time** (which tools, which model, which permissions)
- **No terminal output scanning needed** — the chat API handles all communication
- **No trust gate** — agents are already configured and trusted
- **No tool permission gate** — tool permissions are defined in `AgentToolSettings`

This makes the worker implementation significantly simpler:

```
dASS Worker:
  Spawning → Running → Completed | Failed | Cancelled

Claw Code Worker:
  Spawning → TrustRequired? → ToolPermissionRequired? → ReadyForPrompt → Running → Finished | Failed
  (with scanning terminal output, detecting prompts, resolving gates, handling misdelivery)
```

---

## 8. Implementation Plan

### Phase 1: Core Worker Service

1. Create `IWorkerService` / `WorkerService` in `LLM/Services/`
2. Implement `CreateWorkerAsync` — creates a Chat, sends prompt, runs agent in background
3. Implement `GetWorkerAsync`, `ListWorkersAsync`, `StopWorkerAsync`
4. Implement `AwaitWorkerAsync` with timeout

### Phase 2: Tool Module

1. Create `WorkerToolModule` with `worker-create`, `worker-get`, `worker-list`, `worker-await`, `worker-stop`
2. Register in DI as `[ToolModule]`

### Phase 3: Observability & Events

1. Add `WorkerEvent` system for tracking lifecycle
2. Show worker status in UI (maybe in a sidebar or tab)
3. Allow clicking on a worker to open its chat session

### Phase 4: Advanced Features

1. **Task Packets** — structured task definitions (like Claw Code's `TaskPacket`)
2. **Teams** — parallel execution across multiple agents
3. **Cron** — scheduled/recurring worker execution
4. **Worker pools** — limit concurrent workers
5. **Result delivery hooks** — push result to parent chat, send notification, trigger webhook

---

## 9. Example Use Cases

### 9.1 Parallel Code Review

```lua
local files = {"src/main.rs", "src/lib.rs", "tests/test.rs"}

-- Launch parallel review workers
local workers = {}
for _, file in ipairs(files) do
    local w = dass.tools.call("worker-create", {
        agent_name = "Reviewer",
        prompt = "Review this file for bugs and security issues: " .. file,
        tools = {"read_file"}
    })
    table.insert(workers, w.content)
end

-- Collect all reviews
for _, wid in ipairs(workers) do
    local result = dass.tools.call("worker-await", { worker_id = wid })
    print(result.content)
end
```

### 9.2 Background Research

```lua
-- User can continue chatting while search runs in background
local w = dass.tools.call("worker-create", {
    agent_name = "WebSearch",
    prompt = "Research the latest trends in Rust async Web frameworks",
    tools = {"web-search", "web-fetch"}
})

-- ... user continues conversation ...
-- ... later, check results ...

local result = dass.tools.call("worker-get", { worker_id = w.content })
if result.status == "Completed" then
    print("Research complete:", result.result_summary)
end
```

### 9.3 CI/CD Pipeline Step

```lua
-- Run tests in background during development
local w = dass.tools.call("worker-create", {
    agent_name = "Coder",
    prompt = "Run 'cargo test' and fix any failures",
    tools = {"bash", "read_file", "edit_file"}
})

-- Await with timeout
local result = dass.tools.call("worker-await", {
    worker_id = w.content,
    timeout_seconds = 300
})

if result.status == "Completed" then
    print("Tests passed!")
else
    print("Tests failed: " .. result.error_message)
end
```
