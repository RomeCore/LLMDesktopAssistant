# Architectural Comparison: dASS vs Claw Code

**Status:** Analysis  
**Author:** Architectural Analysis  
**Date:** 2026-05-26

---

## 1. Core Philosophy

| Aspect | **dASS** | **Claw Code** |
|---|---|---|
| **First Class Citizen** | **Chat** — a multi-user, multi-agent conversation with branching history | **Worker** — an autonomous agent instance with a state machine lifecycle |
| **Primary use case** | Interactive assistant for general tasks, research, coding, multi-user collaboration | Autonomous coding agent for CI/CD, background refactoring, dev workflows |
| **Execution model** | **Sequential** — one agent at a time in a chat; branching is memory, not parallelism | **Fire-and-Forget** — workers run independently, results collected later |
| **UI paradigm** | Desktop GUI (Avalonia) + Web UI (Blazor) | CLI (terminal REPL) |
| **Target audience** | Multiple users + multiple agents in shared context | Single developer + multiple autonomous agents |
| **Extensibility** | Meta Tools (Lua/Python), ToolModules via DI | Plugins (Rust traits), RuntimeToolDefinition |

---

## 2. Chat-Centric vs Worker-Centric

### 2.1 dASS: Chat is Everything

```
User Message → Agent Selection (OrderingService)
    → Agent executes (with tools)
    → Result appended to Chat
    → Next agent selected (or wait for user)
    → Branching creates alternative timelines
```

Key properties:
- **Strict ordering** — messages are sequential; branching is a data structure, not parallel execution
- **Shared context** — all agents and users see (filtered) history
- **Permission model** — per-agent read permissions + per-agent exposure mode
- **Rollback** — branching allows easy navigation to any point

### 2.2 Claw Code: Worker is Everything

```
WorkerCreate → WorkerObserve (scan terminal) → ResolveTrust → SendPrompt → WorkerObserveCompletion
    → Each worker has ITS OWN session
    → Workers run in parallel via TeamCreate
    → Results polled via TaskGet/TaskOutput
```

Key properties:
- **Isolated execution** — each worker has its own session, own context
- **Parallel by design** — teams of workers run simultaneously
- **Terminal-based** — communication via terminal stdout/stderr
- **Failure detection** — scans terminal for trust prompts, permission prompts, misdelivery

### 2.3 Fundamental Difference

dASS is designed for **collaboration** (many users, many agents, shared context).
Claw Code is designed for **automation** (one user, many autonomous agents, isolated contexts).

**Neither is "better"** — they serve different purposes. However, dASS can add worker-like functionality on top of its chat foundation, while Claw Code cannot easily add multi-user shared chat.

---

## 3. Prompt Engineering Comparison

### 3.1 dASS's Strengths

| Feature | dASS Implementation | Claw Code Equivalent |
|---|---|---|
| **Template engine** | LLTSharp — powerful templating with metadata, conditions, loops, localization | String concatenation in `SystemPromptBuilder` |
| **Components** | Reusable prompt blocks (`markdown_tips`, `uncensored`, `git_hints`) selected per-agent | Hardcoded sections |
| **Behavior sliders** | GUI-driven personality adjustment (creativity, formality, conciseness) | `OutputStyle` — simple text override |
| **Personas** | Predefined role templates with names and descriptions | ❌ None |
| **Specializations** | Domain-specific knowledge add-ons | ❌ None |
| **Localization** | Multi-language prompts via `@metadata lang` | ❌ English only |
| **UI editor** | `PromptManagerView` — graphical prompt management | ❌ CLI only |
| **Injectors & Hooks** | `IPromptInjector` + `IPromptBuildingHook` — DI-based extensibility | `append_section()` in Rust code |
| **Multi-agent filtering** | Per-agent `ReadPermissions` + `ExposureMode` | ❌ All agents see everything |

### 3.2 Claw Code's Strengths

| Feature | Claw Code Implementation | dASS Equivalent |
|---|---|---|
| **Git context** | Automatic git status, diff, commits injected into prompt | ❌ None (planned: `GitContextExpander`) |
| **CLAUDE.md** | Hierarchical instruction files from file system | ❌ None (planned: `ClaudeMdExpander`) |
| **Dynamic boundary** | `__SYSTEM_PROMPT_DYNAMIC_BOUNDARY__` marker for compactification | ❌ None (can be added) |
| **Project context** | Working directory, date, OS, platform | ❌ None (planned: `EnvironmentExpander`) |
| **Runtime config** | Serialized `.claw.json` in prompt | ❌ Not needed (settings are UI-based) |
| **Template functions** | ❌ None | ❌ None (planned: `ITemplatePlugin`) |

### 3.3 Why dASS's Approach is More Powerful

The **LLT template engine** combined with **DI-based extensibility** (`IPromptInjector`, `IPromptBuildingHook`, planned `IPromptContextExpander`, `ITemplatePlugin`) creates a **composition system** that Claw Code's hardcoded `SystemPromptBuilder` cannot match:

```handlebars
{{! dASS: choose components based on agent }}
@template system_prompt {
  @foreach component in components {
    @component  {{! dynamically selected per agent }}
  }
  
  @foreach section in context_sections {
    # @section.Title
    @section.Content  {{! from IPromptContextExpander[] }}
  }
  
  @if persona { @persona }
  @if specialization { @specialization }
}
```

vs Claw Code's fixed structure:
```rust
// Claw Code: hardcoded order, hardcoded content
fn build(&self) -> Vec<String> {
    vec![
        get_intro_section(),
        get_system_section(),
        get_doing_tasks_section(),
        get_actions_section(),
        SYSTEM_PROMPT_DYNAMIC_BOUNDARY,
        self.environment_section(),
        render_project_context(...),
        render_instruction_files(...),
        render_config_section(...),
    ]
}
```

By adding `IPromptContextExpander[]` and `ITemplatePlugin[]`, dASS would surpass Claw Code in every dimension of prompt engineering flexibility.

---

## 4. Tool System Comparison

### 4.1 dASS: Modular, Decorated, Auto-Schemed

- Each tool is a C# method in a `[ToolModule]` class
- JSON Schema is **auto-generated** from method signature via `ToolExecutorCreator`
- Return types: `ReactiveToolResult`, `ToolResult`, `string`, `void`, `Task<>`
- Dynamic descriptions via `DescriptionGetter`
- Streaming progress via `ReactiveToolResult`
- Tools are added per-agent via `AgentToolSettings`

### 4.2 Claw Code: Monolithic, Hand-Schemed, Match-Based

- All tools in a single 9891-line `lib.rs` file
- JSON Schema is **hand-written** with `json!()` macros
- Return type: `Result<String, String>`
- Execution via giant `match` statement in `execute_tool_with_enforcer()`
- Permission system (`ReadOnly` / `WorkspaceWrite` / `DangerFullAccess`)

### 4.3 Coverage Comparison

| Category | dASS | Claw Code |
|---|---|---|
| **File read** | `fs-read_entry` (with line range) | `read_file` (with offset/limit) |
| **File write** | `fs-write_file` / `fs-write_binary_file` | `write_file` |
| **File edit** | `fs-apply_diff` / `fs-replace` (string+regex) | `edit_file` (old→new string) |
| **File info** | `fs-get_file_info` | ❌ (delegated to bash) |
| **File delete/copy/move** | ✅ Dedicated tools | ❌ (delegated to bash) |
| **Directory create/delete** | ✅ Dedicated tools | ❌ (delegated to bash) |
| **Grep search** | `fs-grep` | `grep_search` (richer: multiline, context, glob) |
| **Glob search** | ❌ (via fs-read_entry) | `glob_search` |
| **Document reading** | `fs-read_document_file` (PDF/DOCX/PPTX) | PDF only (via `pdf_extract.rs`) |
| **Web request** | `web-request` (GET/POST/PUT/DELETE) | `RemoteTrigger` (similar) |
| **Web fetch** | `web-fetch` (HTML/Markdown) | `WebFetch` (with prompt extraction) |
| **Web search** | `web-search` (80+ engines) | `WebSearch` (with domain filters) |
| **Web parse** | `web-parse` (CSS selector) | ❌ |
| **Math** | `calculate` (complex numbers, integrals) | ❌ (delegated to bash/python) |
| **Random** | 8 tools (GUID, dice, coin, chance, etc.) | ❌ (delegated to bash) |
| **Time** | `time-get`, `time-wait` (with progress) | `Sleep` (only delay) |
| **Forms (HITL)** | 4 tools (confirm, choice, input, file picker) | `AskUserQuestion` (simple stdin) |
| **Agent call** | `agent-ask_question`, `agent-call` | `Agent` |
| **Image description** | `agent-describe_image` | ❌ |
| **Meta Tools** | **5 tools** (create/list/info/rename/delete) | ❌ (only plugins) |
| **Lua execution** | `execute-lua` | ❌ |
| **Python execution** | 3 tools (python, venv, packages) | ❌ (delegated to bash) |
| **Shell execution** | `execute-shell` (Desktop) | `bash` (primary tool, with sandbox) |
| **PowerShell** | `execute-powershell` (Desktop) | `PowerShell` |
| **REPL** | ❌ | `REPL` |
| **Bash** | ❌ (via shell tool) | `bash` (core tool, with validation + sandbox) |
| **Todo tracking** | ❌ | `TodoWrite` |
| **Workers** | ❌ | 9 tools (Create/Get/Observe/ResolveTrust/Await/SendPrompt/Restart/Terminate/Completion) |
| **Tasks** | ❌ | 6 tools (Create/Get/List/Stop/Update/Output, plus TaskPacket) |
| **Teams** | ❌ | 2 tools (Create/Delete) |
| **Cron** | ❌ | 3 tools (Create/Delete/List) |
| **LSP** | ❌ | `LSP` (symbols, references, diagnostics) |
| **Notebook** | ❌ | `NotebookEdit` |
| **MCP** | `MCPToolModule` (auto-conversion) | 4 tools (MCP, ListMcpResources, ReadMcpResource, McpAuth) |
| **Plugins** | ❌ | Plugin system (install/enable/disable) |
| **Config** | ❌ | `Config` (get/set settings) |
| **Plan mode** | ❌ | `EnterPlanMode` / `ExitPlanMode` |
| **Structured output** | ❌ | `StructuredOutput` |
| **Search tools** | ❌ | `ToolSearch` (deferred tool discovery) |

### 4.4 Key Takeaways

| Insight | Implication |
|---|---|
| **dASS has more specialized tools** (math, random, forms, meta tools, binary files, documents) | Better for general-purpose assistance beyond coding |
| **Claw Code has more automation tools** (workers, tasks, teams, cron, todo, LSP, notebook) | Better for CI/CD and development workflows |
| **Claw Code delegates to bash** for many operations (cp, mv, rm, mkdir, python, math) | Simpler tool surface but depends on bash availability |
| **dASS has Meta Tools** — LLM-created tools in Lua/Python | Uniquely powerful: LLM can extend its own capabilities at runtime |
| **dASS has streaming results** (`ReactiveToolResult` with progress, icons, status) | Better UX for long-running operations |
| **Claw Code has permission levels** (ReadOnly/WorkspaceWrite/DangerFullAccess) | More granular security model |

---

## 5. Scripting & Extensibility

| Aspect | dASS | Claw Code |
|---|---|---|
| **Embedded scripting** | ✅ Lua (MoonSharp) + Python (external process) | ❌ Bash only |
| **Meta Tools** | ✅ LLM creates tools at runtime | ❌ Only pre-compiled plugins |
| **Create tool from chat** | `metatools-create_or_update` | ❌ Must write Rust plugin |
| **Lua API surface** | 17 namespaces (fs, web, crypto, json, regex, agents, tools, models, log, os, datetime, string, table, random, manuals) | ❌ N/A |
| **dass.agents.execute()** | ✅ Call LLM with tools from Lua | ❌ N/A |
| **dass.tools.call()** | ✅ Call any tool from Lua | ❌ N/A |
| **Plugin system** | ❌ (Meta Tools fill this niche) | ✅ Plugin lifecycle management |
| **Plugin installation** | ❌ | `claw plugin install <path>` |

---

## 6. What dASS Can Learn from Claw Code

### High Priority

1. **Git context in prompts** — `GitContextExpander` (implement as `IPromptContextExpander`)
2. **CLAUDE.md / instruction files** — `ClaudeMdExpander` (same interface)
3. **Environment context** — `EnvironmentExpander` (platform, date, working directory)
4. **Dynamic boundary marker** — split static/dynamic prompt sections for compactification
5. **Permission levels** — `ReadOnly` / `WorkspaceWrite` / `DangerFullAccess` (beyond simple bool)

### Medium Priority

6. **Worker system** — fire-and-forget background agents (naturally fits chat architecture)
7. **Todo tracking** — structured task list management
8. **LSP integration** — code intelligence from Lua scripting
9. **Structured output** — return structured data alongside text

### Low Priority

10. **Cron / scheduled tasks** — can be implemented via worker system
11. **Teams / parallel execution** — can be implemented via worker system
12. **Plugin system** — Meta Tools already cover most use cases; native plugins would overlap

---

## 7. What Claw Code Can Learn from dASS

1. **Meta Tools** — LLM-created tools are a game-changer for extensibility
2. **Streaming results** — `ReactiveToolResult` with progress, icons, status titles
3. **UI forms** — Human-in-the-Loop (confirm, choice, input, file picker)
4. **Multi-agent permission system** — `ReadPermissions` + `ExposureMode`
5. **Behavior sliders** — personality adjustment without prompt editing
6. **Personas & Specializations** — role-based prompt configuration
7. **Localization** — multi-language prompts
8. **Modular tool organization** — not a single 10K-line file

---

## 8. Architectural Convergence

Both projects are evolving toward similar goals from different directions:

```
dASS (Chat-Centric)
    → adding workers, git context, task management
    → becoming more autonomous

Claw Code (Worker-Centric)
    → adding chat-like session persistence, UI
    → becoming more interactive

Convergence Point:
    A system that combines:
    - Interactive multi-user chat (dASS)
    - Autonomous background workers (Claw Code)
    - Rich scripting & meta tools (dASS)
    - Sandbox & permission system (Claw Code)
    - Comprehensive prompt engineering (dASS + planned expanders)
    - CI/CD integration (Claw Code)
```

**dASS is uniquely positioned** because its chat-centric architecture can naturally accommodate worker-style execution (each worker = separate chat), while Claw Code's worker-centric architecture would require fundamental redesign to support multi-user shared context.
