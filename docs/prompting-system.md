# Prompting System

> **Part of dASS (Desktop Assistant) documentation**  
> Covers the LLT template engine, prompt building pipeline, extensibility, and template management.

---

## 1. Overview

The prompting system is responsible for transforming chat history and agent configuration into a structured prompt for the LLM. It features:

- **LLTSharp** — a powerful template engine with conditions, loops, metadata, and localization
- **Component-based prompt assembly** — reusable blocks (personas, specializations, sliders, components)
- **Multi-layer extensibility** — injectors, hooks, context expanders, and template plugins
- **Granular visibility control** — per-agent permissions and exposure modes filter every message

```
Template Files (.llt)
    │
    ▼
PromptRegistry (loads and indexes all templates)
    │
    ▼
PromptChatBuilder (builds the context for a specific agent)
    │
    ▼
List<IMessage> (ready for LLM API)
```

---

## 2. LLT Template Engine

LLT (LLM Template Language) is a custom templating language with rich features.

### 2.1 Syntax Reference

```llt
@/ This is a comment

@template template_name              @/ Define a template
{
    @metadata                        @/ Template metadata
    {
        guid: '550e8400-e29b-41d4-a716-446655440000',
        lang: 'en-US',
        type: 'component'
    }

    @if variable                      @/ Conditional
    {
        Value: @variable              @/ Variable substitution
    }

    @foreach item in collection       @/ Loop
    {
        - @item.property
    }

    @if condition1 && condition2      @/ Combined conditions
    {
        Both are true
    }
}

@messages template name               @/ Multi-message template (for chat)
{
    @system message { ... }
    @user message { ... }
}
```

### 2.2 Template Types

| Type | Extension | Purpose |
|---|---|---|
| `@template` | `.llt` | Single text template (system prompt, components) |
| `@messages template` | `.llt` | Multi-message template (router prompt with system + user turns) |

### 2.3 Metadata System

Each template carries metadata for indexing and filtering:

```llt
@metadata
{
    guid: '550e8400-e29b-41d4-a716-446655440000',   {{! Unique identifier }}
    lang: 'en-US',                                    {{! Language code }}
    type: 'component'                                  {{! Template type }}
}
```

Supported types: `component`, `persona`, `specialization`, `slider`, or none (regular templates).

### 2.4 Template Functions

Plugins can provide custom functions usable in templates via `IPromptTemplatePlugin`:

```csharp
public interface IPromptTemplatePlugin
{
    IEnumerable<TemplateFunction> GetTemplateFunctions();
}
```

Example implementation — `FilesystemPromptTemplatePlugin` provides `@fs-*` functions.

---

## 3. Built-in Templates

Located in `Prompting/Resources/` and embedded into the assembly.

### 3.1 System Prompt (`system_prompt.llt`)

The main template for building an agent's system message:

```llt
@template system_prompt
{
    @metadata { lang: 'en-US' }

    @if prompt { @prompt }

    @foreach component in components
    { @component }

    @foreach slider in sliders
    { @slider }

    @if persona { Act as: @persona }
    @if specialization { Your specialization is: @specialization }
    @if assistantNickname { Your name: @assistantNickname }
    @if summary { Summary of previous messages: @summary }
}
```

### 3.2 User Message (`user_prompt.llt`)

Formats a user message for LLM consumption:

```llt
@template user_message_prompt
{
    [USER MESSAGE FROM: @user_name]
    [MESSAGE SENT AT: @time_sent]

    @if can_read_attachments && attachments
    {
        [ATTACHMENTS]
        @foreach attachment in attachments { ... }
    }

    @if can_read_content
    {
        [CONTENT]
        @content
    }
}
```

### 3.3 Foreign Assistant Message (`foreign_assistant_prompt.llt`)

Formats messages from other agents (when not identifying as user):

```llt
@template foreign_assistant_prompt
{
    [OTHER AGENT MESSAGE FROM: '@agent_name']:

    @if can_read_reasoning && reasoning_content { [REASONING]: @reasoning_content }
    @if can_read_content { [CONTENT]: @content }

    @if tool_calls
    {
        @if can_read_tool_calls
        {
            @foreach tool_call in tool_calls
            { - @tool_call.name: [ARGUMENTS] @tool_call.result_content }
        }
        else
        {
            @foreach tool_call in tool_calls
            { - @tool_call.name: [ARGUMENTS] [RESULT HIDDEN] }
        }
    }
}
```

### 3.4 Components (`components.llt`)

Reusable prompt blocks selectable per agent:

| Component GUID | Name | Purpose |
|---|---|---|
| `6873aa10-...` | `markdown_tips` | Instructions for using Markdown formatting |
| `81f0998b-...` | `app_hint` | Tells the LLM it's inside the dASS application |
| `ccdf6c29-...` | `multiple_tools` | How to call multiple tools in one message |
| `bcea3e14-...` | `uncensored` | Removes content restrictions |
| `c0f3b7e8-...` | `no_restrictions` | Removes all ethical/content boundaries |
| `fb6c9a5d-...` | `git_hints` | Git command usage instructions |
| `a1b2c3d4-...` | `quick_actions` | Quick action button syntax |
| `b2c3d4e5-...` | `quick_explanations` | Inline tooltip syntax (`@[Term](definition)`) |

### 3.5 Personas (`personas.llt`)

50+ predefined personality templates. Each has a name and description:

```llt
@template friendly_helper
{
    @metadata { guid: '...', type: 'persona', lang: 'en-US' }
    You are a friendly and helpful assistant. You communicate warmly...
}
```

### 3.6 Specializations (`specializations.llt`)

20+ domain-specific knowledge add-ons:

```llt
@template software_architecture
{
    @metadata { guid: '...', type: 'specialization', lang: 'en-US' }
    You specialize in software architecture. You are an expert in design patterns...
}
```

### 3.7 Behavior Sliders (`sliders.llt`)

Adjustable personality dimensions controllable via GUI:

```llt
@template creativity_slider
{
    @metadata { guid: '...', type: 'slider', lang: 'en-US' }

    @if sliderValue >= 0.7
    {
        Be creative and think outside the box. Don't be afraid to suggest unconventional solutions.
    }
    @else if sliderValue <= 0.3
    {
        Stick to conventional approaches and proven solutions.
    }
}
```

Other sliders include: formality, conciseness, verbosity, humor, empathy, technical_depth.

### 3.8 Router Prompt (`router.llt`)

Used by `AdaptiveAgentExecutionStage` for agent selection:

```llt
@messages template router_prompt
{
    @system message
    {
        You are an intelligent agent router...
        Rules: analyze context, consider executed agents,
        match request to capabilities, return @AgentName or None.
        Examples: ...
    }
    @user message
    {
        @context
        [AGENTS]
        @foreach agent in agents { - @agent.name: @agent.description }
    }
}
```

### 3.9 Summarizer (`summarizer.llt`)

Template for compressing long conversation history.

---

## 4. PromptRegistry

A static class that loads, indexes, and provides access to all templates.

### 4.1 Initialization

```csharp
static PromptRegistry()
{
    // 1. Import from all observed assemblies (embedded .llt files)
    foreach (var assembly in ReflectionUtility.ObservedAssemblies)
        SharedLibrary.ImportFromAssembly(assembly);

    // 2. Import from user templates folder
    SharedLibrary.ImportFromFolder(Directories.Templates, recursive: true);

    // 3. Set language fallback
    SharedLibrary.SetLanguageFallbackScheme(new HierarchicalLanguageFallbackScheme());

    // 4. Index by type and GUID
    // → AllBuiltinComponents, AllBuiltinPersonas, etc.
}
```

### 4.2 Public Collections

```csharp
public static class PromptRegistry
{
    public static TemplateLibrary SharedLibrary { get; }

    // All templates by (Guid, LanguageCode)
    public static ImmutableDictionary<(Guid, LanguageCode), PromptComponent> AllBuiltinComponents { get; }
    public static ImmutableDictionary<(Guid, LanguageCode), Persona> AllBuiltinPersonas { get; }
    public static ImmutableDictionary<(Guid, LanguageCode), Specialization> AllBuiltinSpecializations { get; }
    public static ImmutableDictionary<(Guid, LanguageCode), BehaviourSlider> AllBuiltinSliders { get; }

    // Best-match for current language
    public static ImmutableDictionary<Guid, PromptComponent> BuiltinComponents { get; }
    public static ImmutableDictionary<Guid, Persona> BuiltinPersonas { get; }
    public static ImmutableDictionary<Guid, Specialization> BuiltinSpecializations { get; }
    public static ImmutableDictionary<Guid, BehaviourSlider> BuiltinSliders { get; }
}
```

### 4.3 Language Fallback

Uses `HierarchicalLanguageFallbackScheme`:
- Exact match (e.g., `en-US` → `en-US`)
- Language-only match (e.g., `en-US` → `en`)
- Any language fallback

---

## 5. PromptChatBuilder — Building the Context

`PromptChatBuilder` is the central class that assembles the full LLM context for a given agent. It is a **per-chat service** injected with:

```csharp
public class PromptChatBuilder(
    Chat chat,
    TemplateLibrary templates,
    IAgentManagementService agentManager,
    IUserManagementService userManager,
    IEnumerable<IPromptInjector> promptInjectors,
    IEnumerable<IPromptBuildingHook> promptBuildingHooks,
    IEnumerable<IPromptSystemContextExpander> promptSystemContextExpanders,
    IEnumerable<IPromptMessageContextExpander> promptMessageContextExpanders,
    IEnumerable<IPromptTemplatePlugin> promptTemplatePlugins
) : IPromptChatBuilder
```

### 5.1 Build Pipeline

```
chat.Messages (full history)
    │
    ▼
1. IPromptInjector[].Inject()
   → Insert virtual RawUserMessage instances (reactions, system events)
    │
    ▼
2. GroupMessagesIntoRounds()
   → Group messages by rounds (user message + subsequent assistant messages)
   → Apply MaxVisibleRounds filter
    │
    ▼
3. IPromptBuildingHook[].Modify()
   → Transform, replace, or remove individual BranchedMessages
    │
    ▼
4. Visibility Filtering (per message):
   a. ContextShield check → break if found
   b. UserMessage → IsUserMessageVisibleToAgent()
      - Check UserMessages permission
      - Check Visibility mode (OnlyUsers, OnlyAgents, Always)
      - Check white/black list
   c. AssistantMessage → IsAssistantMessageVisibleToAgent()
      - Own messages → check OwnMessages permission
      - Foreign messages → check OtherAgentMessages
      - Tool calls → check both sender's exposure and receiver's permissions
      - Agent ID filter → check white/black list
    │
    ▼
5. Summary Check
   → If message has SummaryViewModel (completed) → summarize previous messages
   → System prompt gets `summary` variable
    │
    ▼
6. ConvertMessageForAgent()
   a. Own assistant message → full fidelity (AssistantMessage + ToolMessage)
   b. Foreign assistant message → quoted UserMessage (via foreign_assistant_prompt)
      - If IdentifySelfAsUser → user_message_prompt instead
   c. User message → UserMessage (via user_prompt or user_message_prompt)
    │
    ▼
7. IPromptBuildingHook[].ModifyFinalContext()
   → Final edits on the converted message list
    │
    ▼
8. Build System Prompt
   → Render system_prompt.llt with:
      • prompt, components, sliders, persona, specialization,
        assistantNickname, summary
      • Context from IPromptSystemContextExpander[]
    │
    ▼
9. Insert SystemMessage at index 0
    │
    ▼
Output: List<IMessage> ready for LLM API
```

### 5.2 Message Conversion Rules

| Original Message Type | Agent Is Sender? | Output Format |
|---|---|---|
| `UserMessage` | N/A | `UserMessage` (rendered via `user_prompt.llt`) |
| `AssistantMessage` | Yes (own) | `AssistantMessage` + `ToolMessage` (full fidelity) |
| `AssistantMessage` | No (foreign) | `UserMessage` (rendered via `foreign_assistant_prompt.llt` or `user_prompt.llt` if masked) |

### 5.3 System Prompt Building

```csharp
private string BuildSystemPrompt(string? summaryOfPrevMessages, AgentDescriptor agent)
{
    var template = templates.Retrieve("system_prompt", language);
    
    var context = new Dictionary<string, object?>();
    
    // Context expanders contribute variables
    foreach (var expander in promptSystemContextExpanders)
        expander.ExpandPromptContext(context);
    
    context["prompt"] = agent.Prompts.SystemPrompt;
    context["components"] = agent.Prompts.PromptComponents
        .Select(id => PromptRegistry.GetComponent(id)?.Render(context));
    context["sliders"] = agent.Prompts.SliderValues
        .Select(s => PromptRegistry.GetSlider(s.SliderId)?.Render(new { sliderValue = s.Value }));
    context["persona"] = agent.Prompts.Persona ?? agent.Prompts.CustomPersona;
    context["specialization"] = agent.Prompts.Specialization ?? agent.Prompts.CustomSpecialization;
    context["assistantNickname"] = agent.Prompts.Nickname;
    context["summary"] = summaryOfPrevMessages;
    
    return template.Render(context, functions);
}
```

---

## 6. Extensibility Points

### 6.1 `IPromptInjector`

Insert **virtual messages** into the conversation before building context:

```csharp
public interface IPromptInjector
{
    int Order => 0;  // Execution order (lower first)
    void Inject(List<BranchedMessage> messages, AgentDescriptor agent);
}
```

**Use cases**: message reactions, user profile changes, system events, chat renames.

### 6.2 `IPromptBuildingHook`

**Modify existing messages** or the final LLM context:

```csharp
public interface IPromptBuildingHook
{
    int Order => 0;
    
    // Modify a message before conversion (return null to remove)
    BranchedMessage? Modify(BranchedMessage message, AgentDescriptor agent) => message;
    
    // Modify final messages after conversion
    IEnumerable<IMessage>? ModifyFinalContext(
        IEnumerable<IMessage> messages, BranchedMessage message, AgentDescriptor agent) => null;
}
```

**Use cases**: message replacement, removal, annotation, branch switching.

### 6.3 `IPromptSystemContextExpander`

Add variables to the **system prompt rendering context**:

```csharp
public interface IPromptSystemContextExpander
{
    void ExpandPromptContext(Dictionary<string, object?> context);
}
```

**Use cases**: environment info, current date/time, platform, working directory.

### 6.4 `IPromptMessageContextExpander`

Add variables to **message rendering context**:

```csharp
public interface IPromptMessageContextExpander
{
    void ExpandPromptContext(
        BranchedMessage message, AgentDescriptor? agent,
        Dictionary<string, object?> context);
}
```

**Use cases**: message-specific metadata, user info, attachment details.

### 6.5 `IPromptTemplatePlugin`

Provide **custom template functions** usable inside `.llt` templates:

```csharp
public interface IPromptTemplatePlugin
{
    IEnumerable<TemplateFunction> GetTemplateFunctions();
}
```

**Built-in implementation**: `FilesystemPromptTemplatePlugin` — adds `@fs-*` functions.

---

## 7. Behavior Sliders System

Sliders provide **GUI-adjustable personality knobs** for each agent.

### 7.1 Definition (in `sliders.llt`)

Each slider is a template that receives a `sliderValue` (0.0–1.0):

```handlebars
@template formality_slider
{
    @metadata { guid: '...', type: 'slider', lang: 'en-US' }
    @if sliderValue >= 0.7
    {
        Use formal language, proper grammar, and professional tone.
        Avoid slang, contractions, and casual expressions.
    }
    @else if sliderValue <= 0.3
    {
        Feel free to use casual language, slang, and informal expressions.
    }
}
```

### 7.2 Storage

Each agent stores slider values in `AgentPromptSettings.SliderValues`:
```csharp
public ICollection<BehaviorSliderValue> SliderValues { get; set; }
// BehaviorSliderValue = { Guid SliderId, int Value }
```

### 7.3 Rendering

During `BuildSystemPrompt()`, active sliders are rendered with their current values and included in the system prompt.

---

## 8. Context Summarization

When the conversation grows long, `ChatSummarizationService` can compress older messages:

1. A `SummaryViewModel` is attached to a message in the history
2. During `Build()`, if `AllowSummaries = true` and a completed summary is found:
   - The summary text is passed as `summary` to the system prompt
   - Older messages are skipped
3. This keeps context within token limits without losing important context

---

## 9. PromptManager UI

Located in `Prompting/PromptManagerView.axaml` / `PromptManagerViewModel.cs`:

- **Components tab** — browse, enable/disable built-in components
- **Personas tab** — select from predefined or create custom
- **Specializations tab** — select from predefined or create custom
- **Sliders tab** — adjust slider values with GUI controls
- **Custom prompts** — write and test custom system prompts

The UI communicates with `PromptRegistry` and `AgentsConfiguration` to persist settings.

---

## 10. Localization

All templates support localization via `@metadata { lang: '...' }`:

```handlebars
@template system_prompt
{
    @metadata { lang: 'ru-RU' }
    Ты — полезный ассистент...
}
```

The `HierarchicalLanguageFallbackScheme` resolves templates:
1. Exact locale match (`ru-RU`)
2. Language match (`ru`)
3. Default (`en-US`)
4. Any available

Template resolution is culture-aware:
```csharp
var language = new LanguageMetadata(new LanguageCode(CultureInfo.CurrentCulture));
var template = templates.TryRetrieveBestAllWithFallback("system_prompt", language);
```

---

## 11. Architecture Diagram

```
                    ┌───────────────────────────┐
                    │    .llt Template Files    │
                    │  (embedded + user folder) │
                    └────────────┬──────────────┘
                                 │
                                 ▼
                    ┌───────────────────────────┐
                    │     PromptRegistry        │
                    │  (indexes by GUID, lang)  │
                    └────────────┬──────────────┘
                                 │
                    ┌────────────┴────────────┐
                    │                         │
                    ▼                         ▼
        ┌─────────────────────┐   ┌──────────────────────┐
        │ IPromptInjector[]   │   │ IPromptTemplatePlugin│
        │ (virtual messages)  │   │ (template functions) │
        └──────────┬──────────┘   └──────────────────────┘
                   │
                   ▼
        ┌──────────────────────────────────────────────┐
        │          PromptChatBuilder.Build(agent)      │
        │                                              │
        │  1. Inject virtual messages                  │
        │  2. Group into rounds                        │
        │  3. Apply IPromptBuildingHook.Modify()       │
        │  4. Filter visibility (read + expose)        │
        │  5. Check summaries                          │
        │  6. Convert messages (per agent)             │
        │  7. Apply IPromptBuildingHook.ModifyFinal()  │
        │  8. Build system prompt                      │
        │     - IPromptSystemContextExpander[]         │
        │     - Components, sliders, persona, etc.     │
        │  9. Insert SystemMessage                     │
        └───────────────────┬──────────────────────────┘
                            │
                            ▼
                    ┌──────────────────────┐
                    │  List<IMessage>      │
                    │  → ready for LLM API │
                    └──────────────────────┘
```

---

## 12. Best Practices

1. **Use components for reusable instructions** — avoid duplicating common instructions
2. **Set personas and specializations** — they significantly improve response quality
3. **Adjust sliders per agent** — different agents may need different creativity/formality levels
4. **Use localization** — templates auto-resolve based on culture
5. **Create custom `.llt` files** in `Directories.Templates` for user-defined templates
6. **Use `IPromptSystemContextExpander`** for dynamic context (time, weather, platform)
7. **Keep system prompts concise** — use components to modularize
8. **Test with `PromptManager` UI** — preview how the final prompt looks
