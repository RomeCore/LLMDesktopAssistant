# IPromptContextExpander & ITemplatePlugin — Context Expansion & Template Extensibility

**Status:** Proposal  
**Author:** Architectural Analysis  
**Date:** 2026-05-26

---

## 1. Motivation

The current prompting system (LLT + `IPromptInjector` + `IPromptBuildingHook`) is already more powerful than competitors (Claw Code, Claude Code), but there are two gaps:

1. **No dynamic runtime context** — Claw Code automatically injects git status, git diff, CLAUDE.md, platform info, etc. We can only do this via `IPromptInjector`, which requires writing C# code for each case.

2. **No LLT extensibility** — the LLTSharp template engine doesn't support user-defined functions (e.g. `@git_status`, `@now`, `@read_file`).

---

## 2. IPromptContextExpander — Context Expanders

### 2.1 Interface

```csharp
/// <summary>
/// Expands the context delivered into an agent's prompt.
/// Executes after IPromptInjector and IPromptBuildingHook,
/// before the final system prompt assembly.
/// </summary>
public interface IPromptContextExpander
{
    /// <summary>Execution order. Lower values run first.</summary>
    int Order => 0;

    /// <summary>
    /// Returns Markdown sections appended to the system prompt
    /// after the __DYNAMIC_BOUNDARY__ marker.
    /// </summary>
    IEnumerable<ContextSection> Expand(Guid agentId, CancellationToken cancellationToken = default);
}

/// <summary>
/// A named chunk of Markdown content injected into the prompt.
/// </summary>
public record ContextSection
{
    /// <summary>Section heading (wrapped in ## or #).</summary>
    public required string Title { get; init; }
    
    /// <summary>Section content in Markdown.</summary>
    public required string Content { get; init; }
    
    /// <summary>
    /// Sort priority. Lower = earlier in prompt.
    /// Convention: Environment=0, Project=10, Instructions=20, Config=30
    /// </summary>
    public int Priority { get; init; } = 10;
}
```

### 2.2 Example Implementations

```csharp
// Git context (like Claw Code's ProjectContext)
public class GitContextExpander : IPromptContextExpander
{
    public IEnumerable<ContextSection> Expand(Guid agentId, CancellationToken ct)
    {
        var sections = new List<ContextSection>();
        
        var status = ExecuteGit("status --short --branch");
        if (!string.IsNullOrEmpty(status))
            sections.Add(new ContextSection("Git Status", $"```\n{status}\n```") { Priority = 20 });

        var diff = ExecuteGit("diff");
        if (!string.IsNullOrEmpty(diff))
            sections.Add(new ContextSection("Git Diff", $"```diff\n{diff}\n```") { Priority = 21 });

        var log = ExecuteGit("log --oneline -10");
        if (!string.IsNullOrEmpty(log))
            sections.Add(new ContextSection("Recent Commits", $"```\n{log}\n```") { Priority = 22 });

        return sections;
    }
}

// CLAUDE.md from directory hierarchy (Claw Code style)
public class ClaudeMdExpander : IPromptContextExpander
{
    public IEnumerable<ContextSection> Expand(Guid agentId, CancellationToken ct)
    {
        var chat = services.GetRequiredService<Chat>();
        var cwd = chat.Settings.Environment.GetWorkingDirectory();
        
        return DiscoverInstructionFiles(cwd)
            .Select(f => new ContextSection(
                $"{f.FileName} (scope: {f.Scope})",
                Truncate(f.Content, 4000))
            { Priority = 30 });
    }
}

// Environment info
public class EnvironmentExpander : IPromptContextExpander
{
    public IEnumerable<ContextSection> Expand(Guid agentId, CancellationToken ct)
    {
        return [new ContextSection("Environment", $"""
            - **Model family:** {ResolveModelName(agentId)}
            - **Working directory:** `{GetWorkingDirectory()}`
            - **Date:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}
            - **Platform:** {RuntimeInformation.OSDescription}
            - **OS Architecture:** {RuntimeInformation.OSArchitecture}
        """) { Priority = 0 }];
    }
}

// Available agents roster (for router)
public class AgentRosterExpander : IPromptContextExpander
{
    public IEnumerable<ContextSection> Expand(Guid agentId, CancellationToken ct)
    {
        var agents = agentManager.ListAgents()
            .Select(a => $"- **@{a.Agent.Info.Name}**: {a.Agent.Info.Description}");
        
        return [new ContextSection("Available Agents", string.Join("\n", agents)) { Priority = 15 }];
    }
}
```

### 2.3 Integration into PromptChatBuilder

In `PromptChatBuilder.Build(Guid agentId)`:

```csharp
public IEnumerable<IMessage> Build(Guid agentId)
{
    // ... existing logic (injectors, hooks, permission filtering) ...

    // New step: context expansion
    var expanders = promptContextExpanders
        .OrderBy(e => e.Order)
        .ToList();
    
    var contextSections = new List<ContextSection>();
    foreach (var expander in expanders)
    {
        contextSections.AddRange(expander.Expand(agentId));
    }
    contextSections = contextSections.OrderBy(s => s.Priority).ToList();

    // Build system prompt with context sections
    var systemPrompt = BuildSystemPrompt(summaryOfPrevMessages, agentId, contextSections);
    
    // ... rest of logic ...
}
```

### 2.4 Updated system_prompt.llt

```handlebars
@template system_prompt
{
    @metadata { lang: 'en-US' }
    
    @if prompt { @prompt }
    
    @foreach component in components { @component }
    @foreach slider in sliders { @slider }
    
    __DYNAMIC_BOUNDARY__
    
    @foreach section in context_sections
    {
        # @section.Title
        @section.Content
    }
    
    @if persona { Act as: @persona }
    @if specialization { Your specialization is: @specialization }
    @if assistantNickname { Your alternate name is: @assistantNickname }
    @if summary { Summary of previous messages: @summary }
}
```

---

## 3. ITemplatePlugin — LLT Template Plugins

### 3.1 Interface

```csharp
/// <summary>
/// A plugin for LLTSharp that adds custom functions
/// to the template engine.
/// </summary>
public interface ITemplatePlugin
{
    /// <summary>
    /// Plugin namespace. Functions become available as
    /// @namespace.function_name in LLT templates.
    /// </summary>
    string Namespace { get; }
    
    /// <summary>
    /// Registers functions with the template engine.
    /// </summary>
    void Register(ITemplateFunctionRegistry registry);
}

/// <summary>
/// Registry for registering functions with LLT.
/// </summary>
public interface ITemplateFunctionRegistry
{
    void RegisterFunction<TResult>(string name, Func<TResult> handler);
    void RegisterFunction<T, TResult>(string name, Func<T, TResult> handler);
    void RegisterFunction<T1, T2, TResult>(string name, Func<T1, T2, TResult> handler);
    // ... etc.
}
```

### 3.2 Example Plugins

```csharp
// Git functions for templates
[TemplatePlugin]
public class GitTemplatePlugin : ITemplatePlugin
{
    public string Namespace => "git";
    
    public void Register(ITemplateFunctionRegistry registry)
    {
        registry.RegisterFunction("status", () => ExecuteGit("status --short --branch"));
        registry.RegisterFunction("diff", () => ExecuteGit("diff"));
        registry.RegisterFunction("log", (int count) => ExecuteGit($"log --oneline -{count}"));
        registry.RegisterFunction("branch", () => ExecuteGit("branch --show-current"));
    }
}

// File system functions
[TemplatePlugin]
public class FileSystemTemplatePlugin : ITemplatePlugin
{
    public string Namespace => "fs";
    
    public void Register(ITemplateFunctionRegistry registry)
    {
        registry.RegisterFunction("read", (string path) => File.ReadAllText(ResolvePath(path)));
        registry.RegisterFunction("list", (string path) => string.Join("\n", Directory.GetFiles(ResolvePath(path))));
        registry.RegisterFunction("exists", (string path) => File.Exists(ResolvePath(path)));
        registry.RegisterFunction("size", (string path) => new FileInfo(ResolvePath(path)).Length.ToString());
    }
}

// Date/time functions
[TemplatePlugin]
public class DateTimeTemplatePlugin : ITemplatePlugin
{
    public string Namespace => "time";
    
    public void Register(ITemplateFunctionRegistry registry)
    {
        registry.RegisterFunction("now", () => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        registry.RegisterFunction("utc", () => DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        registry.RegisterFunction("format", (string format) => DateTime.Now.ToString(format));
        registry.RegisterFunction("unix", () => DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
    }
}

// Web functions
[TemplatePlugin]
public class WebTemplatePlugin : ITemplatePlugin
{
    public string Namespace => "web";
    
    public void Register(ITemplateFunctionRegistry registry)
    {
        registry.RegisterFunction("fetch", async (string url) => {
            using var http = new HttpClient();
            return await http.GetStringAsync(url);
        });
    }
}
```

### 3.3 Usage in LLT Templates

```handlebars
@template custom_prompt
{
    Git status:
    @git.status
    
    Files in src:
    @foreach file in @fs.list("src")
    {
        - @file
    }
    
    Current time: @time.now
    
    Last 5 commits:
    @git.log(5)
}
```

### 3.4 Plugin Registration

```csharp
// In App.axaml.cs or ServiceRegistry initialization
public static void RegisterTemplatePlugins(ITemplateEngine engine, IEnumerable<ITemplatePlugin> plugins)
{
    foreach (var plugin in plugins)
    {
        var registry = engine.CreateFunctionRegistry(plugin.Namespace);
        plugin.Register(registry);
    }
}
```

---

## 4. Complete Prompt Building Pipeline (After Changes)

```
1. Chat.Messages (BranchedMessage[])
    │
    ├── 2. GroupMessagesIntoRounds(maxRounds)
    │       └── Filter by MaxVisibleRounds
    │
    ├── 3. IPromptInjector[].Inject()
    │       └── Insert synthetic messages
    │
    ├── 4. IPromptBuildingHook[].Modify()
    │       └── Modify/remove BranchedMessage entries
    │
    ├── 5. Permission filtering (ReadPermissions + ExposureMode)
    │       └── IsUserMessageVisibleToAgent / IsAssistantMessageVisibleToAgent
    │
    ├── 6. Convert to IMessage[]
    │       └── UserMessage → RCLLM UserMessage
    │       └── AssistantMessage → RCLLM AssistantMessage + ToolMessage
    │       └── ForeignAgent → merged UserMessage
    │
    ├── 7. IPromptContextExpander[].Expand()
    │       └── Collect dynamic context sections
    │
    ├── 8. BuildSystemPrompt()
    │       └── Render system_prompt.llt with:
    │       │   - prompt, components, sliders
    │       │   - context_sections (from expanders)
    │       │   - persona, specialization, summary
    │       │   - ITemplatePlugin functions: @git.status, @time.now, ...
    │
    ├── 9. IPromptBuildingHook[].ModifyFinalContext()
    │       └── Final modification of IMessage[] list
    │
    └── 10. → LLM API (ChatStreamingAsync)
```

---

## 5. Comparison with Claw Code

| Feature | Claw Code | dASS after changes |
|---|---|---|
| **Git status/diff/commits** | Built into SystemPromptBuilder | `GitContextExpander` + `GitTemplatePlugin` |
| **CLAUDE.md / instruction files** | Hierarchical search from cwd up to root | `ClaudeMdExpander` — same approach |
| **Platform/date/time** | `EnvironmentSection` | `EnvironmentExpander` |
| **Dynamic template functions** | ❌ None | ✅ `ITemplatePlugin` — **unique feature** |
| **Prompt management UI** | ❌ CLI only | ✅ `PromptManager` + behavior sliders |
| **Internationalization** | ❌ None | ✅ LLT with `@metadata lang` |
| **Multi-agent permission filtering** | ❌ None | ✅ `ReadPermissions` + `ExposureMode` |
| **Custom prompt sections** | `append_section()` in Rust code | `IPromptContextExpander[]` via DI |

After adding `IPromptContextExpander` and `ITemplatePlugin`, dASS's prompting system becomes **the most flexible** among all known LLM clients (Claude Code, Claw Code, Continue.dev, Aider, etc.).

---

## 6. Implementation Plan

1. **Create interfaces** `IPromptContextExpander` and `ITemplatePlugin` in `LLM/Services/`
2. **Add registration** in `ChatServicesBuilderExtensions` and `ServiceRegistry`
3. **Modify `PromptChatBuilder.Build()`** — add expander invocation
4. **Modify `system_prompt.llt`** — add `__DYNAMIC_BOUNDARY__` and `@foreach section in context_sections`
5. **Create `TemplatePluginRegistry`** — register functions in LLTSharp
6. **Implement base expanders**: `EnvironmentExpander`, `GitContextExpander`, `ClaudeMdExpander`
7. **Implement base plugins**: `GitTemplatePlugin`, `FileSystemTemplatePlugin`, `DateTimeTemplatePlugin`
8. **Document** in `docs/` and add LLT template examples
