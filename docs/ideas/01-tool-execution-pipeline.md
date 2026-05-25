# Tool Execution Pipeline

> **Status:** Proposal  
> **Priority:** High  
> **Inspired by:** Real-world issues with LLM-generated tool calls (broken JSON, secrets leakage, lack of previews)

## Problem

Current tool execution flow is too simple:

```
RCLLM generates tool call → Execute → Return string result
```

This leads to:
- **Broken JSON** arguments (unbalanced brackets, bad escaping) — especially in complex tools like `fs-grep` and `fs-apply_diff`
- **Secrets leakage** — API keys passed as arguments can be returned to the LLM
- **No structured results** — everything is a plain string, hard to parse in Lua/MCP
- **No user preview** — tools execute immediately, user can't see what will happen
- **No macros** — no way to inject dynamic values like `%%LAST_USER_MESSAGE%%`

## Proposed Pipeline

```
┌──────────────┐
│  RCLLM       │  Streaming deltas of tool calls
│  Generation  │  (partial JSON, assembled in real-time)
└──────┬───────┘
       │
       ▼
┌──────────────┐
│  ① Stream    │  Collect streaming deltas from RCLLM
│  Assembly    │  Build complete tool call incrementally
└──────┬───────┘
       │
       ▼
┌──────────────┐
│  ② JSON      │  Auto-fix broken JSON:
│  Repair      │  - Balance brackets [] {}
│              │  - Fix escaped quotes
│              │  - Remove trailing commas
│              │  - Add missing closing brackets
└──────┬───────┘
       │
       ▼
┌──────────────┐
│  ③ Macros    │  Replace %%MACROS%% with real values:
│  Expansion   │  - %%SECRET:OPENAI_API_KEY%%
│              │  - %%LAST_USER_MESSAGE%%
│              │  - %%CHAT_HISTORY%%
│              │  - %%NOW%%, %%WORK_DIR%%, %%RANDOM_UUID%%
│              │  - %%ENV:PATH%%
└──────┬───────┘
       │
       ▼
┌──────────────┐
│  ④ Preview   │  (Optional) Dry-run that shows UI status
│  / Dry-run   │  - Shows icon + status text to user
│              │  - Can prompt for confirmation
│              │  - No actual execution
└──────┬───────┘
       │
       ▼
┌──────────────┐
│  ⑤ Execution │  Actual tool execution via Executor
│              │  - Updates status in real-time (ReactiveToolResult)
│              │  - Shows progress, icon, title
└──────┬───────┘
       │
       ▼
┌──────────────┐
│  ⑥ Secret    │  Sanitize secrets from result:
│  Sanitizer   │  - Replace known secrets with ***
│              │  - Prevents LLM from reading API keys
└──────┬───────┘
       │
       ▼
┌──────────────┐
│  ⑦ Structured│  Return structured result:
│  Result      │  { content, success, tool, status_title,
│              │    status_icon, progress, structured_data }
└──────┬───────┘
       │
       ▼
   Return to LLM / MCP / Lua
```

## Implementation Details

### ① Streaming Assembly

```csharp
public class ToolCallStreamer
{
    private readonly Dictionary<string, StringBuilder> _partialArgs = new();
    
    public void FeedDelta(string toolCallId, string? toolName, string? argsDelta)
    {
        // Append partial JSON
        _partialArgs[toolCallId].Append(argsDelta);
    }
    
    public bool TryComplete(string toolCallId, out JsonNode? args)
    {
        var raw = _partialArgs[toolCallId].ToString();
        args = JsonRepair.Repair(raw);
        return args != null;
    }
}
```

### ② JSON Repair

```csharp
public static class JsonRepair
{
    public static JsonNode? Repair(string raw)
    {
        // Attempt 1: direct parse
        try { return JsonNode.Parse(raw); } catch { }
        
        // Attempt 2: balance brackets
        var fixed = BalanceBrackets(raw);
        try { return JsonNode.Parse(fixed); } catch { }
        
        // Attempt 3: fix escaping + trailing commas
        fixed = FixEscaping(fixed);
        fixed = RemoveTrailingCommas(fixed);
        try { return JsonNode.Parse(fixed); } catch { }
        
        return null; // unrecoverable
    }
}
```

### ③ Macro System

```csharp
public interface IMacroProvider
{
    string? Resolve(string macroName, ToolExecutionContext context);
}

// Built-in providers:
// - SecretMacroProvider  (%%SECRET:...%%)
// - ContextMacroProvider (%%LAST_USER_MESSAGE%%, %%CHAT_HISTORY%%)
// - EnvironmentMacroProvider (%%ENV:...%%)
// - RandomMacroProvider (%%RANDOM_UUID%%, %%RANDOM_INT%%)
```

### ④ Preview / Dry-run

```csharp
// Built-in tool: "preview"
ToolResult PreviewCall(
    [Description("Status text")] string text,
    [Description("Icon name")] string? icon = null,
    [Description("Progress 0-1")] double? progress = null,
    [Description("Show confirmation?")] bool? confirm = null,
    [Description("Prompt text")] string? prompt = null);
```

### ⑤ Structured Result Format

```csharp
public class StructuredToolResult
{
    public string Content { get; init; }
    public bool Success { get; init; }
    public string ToolName { get; init; }
    public string? StatusTitle { get; init; }
    public string? StatusIcon { get; init; }
    public double? Progress { get; init; }
    public JsonObject? StructuredData { get; init; }
}
```

Serialized as JSON for MCP/Lua/RCLLM:
```json
{
  "content": "Heads",
  "success": true,
  "tool": "random-coin_flip",
  "status_title": "Орёл",
  "status_icon": "CircleMultiple"
}
```

## Benefits

- ✅ **Robust** — broken JSON is auto-fixed, fewer errors
- ✅ **Secure** — secrets never reach LLM
- ✅ **User-friendly** — preview before execution, progress during execution
- ✅ **Extensible** — macros work for all tools
- ✅ **Structured** — Lua, MCP, and RCLLM all get parseable results
- ✅ **Backward compatible** — old tools continue working
