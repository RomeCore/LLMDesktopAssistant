# Structured Tool Results

> **Status:** Partially Implemented (Lua API)  
> **Priority:** High  
> **Goal:** Return structured, parseable results from ALL tool executions, not just plain strings.

## Motivation

Currently, tools return plain strings. This causes problems:

- **Lua API** — `dass.tools.call()` now returns structured result ✅, but internal RCLLM still gets a string
- **MCP** — MCP protocol expects structured content (text, images, JSON), but we give it a plain string
- **Chaining** — When one tool's output is input to another, parsing plain strings is fragile
- **Metadata** — Tool status, icons, progress are lost when the result is returned to LLM

## Current State (Lua API — done ✅)

```lua
local r = dass.tools.call("random-coin_flip", {})
-- r = {
--   content = "Heads",
--   success = true,
--   tool = { name = "random-coin_flip", category = "random", ... },
--   status_title = "Орёл",
--   status_icon = "CircleMultiple"
-- }
```

## Goal: Structured Results Everywhere

### Result Format (JSON)

```json
{
  "content": "Heads",
  "success": true,
  "tool_name": "random-coin_flip",
  "status": {
    "title": "Орёл",
    "icon": "CircleMultiple",
    "progress": null
  },
  "structured_data": null,
  "meta": {
    "duration_ms": 15,
    "executed_at": "2026-05-26T00:00:00Z",
    "source": "native",
    "language": null
  }
}
```

### For MCP

MCP already supports structured content. We can return:

```json
{
  "content": [
    {
      "type": "text",
      "text": "Heads"
    },
    {
      "type": "resource",
      "resource": {
        "text": "{\"success\":true,\"status\":{\"title\":\"Орёл\",\"icon\":\"CircleMultiple\"}}",
        "mimeType": "application/json"
      }
    }
  ],
  "isError": false
}
```

### For RCLLM (internal)

The `toolCall.ResultContent` should be:

```json
{
  "result": "Heads",
  "success": true,
  "status_title": "Орёл"
}
```

This gives the LLM structured metadata without breaking existing parsers.

## Implementation

### StructuredResult Class

```csharp
public class StructuredToolResult
{
    /// <summary>
    /// The main textual result content.
    /// </summary>
    public string Content { get; init; } = string.Empty;
    
    /// <summary>
    /// Whether the tool executed successfully.
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// The name of the tool that was called.
    /// </summary>
    public string ToolName { get; init; } = string.Empty;
    
    /// <summary>
    /// Status metadata for UI display.
    /// </summary>
    public ToolStatusInfo? Status { get; init; }
    
    /// <summary>
    /// Optional structured data returned by the tool (e.g., parsed JSON).
    /// </summary>
    public JsonNode? StructuredData { get; init; }
    
    /// <summary>
    /// Execution metadata.
    /// </summary>
    public ToolExecutionMeta? Meta { get; init; }
    
    public string ToJsonString() => JsonSerializer.Serialize(this, _jsonOptions);
    
    public static StructuredToolResult FromReactive(ReactiveToolResult reactive, string toolName)
    {
        return new StructuredToolResult
        {
            Content = reactive.ResultContent,
            Success = reactive.Completion.IsCompletedSuccessfully,
            ToolName = toolName,
            Status = new ToolStatusInfo
            {
                Title = reactive.StatusTitle,
                Icon = reactive.StatusIcon?.ToString(),
                Progress = reactive.Progress
            }
        };
    }
}

public class ToolStatusInfo
{
    public string? Title { get; init; }
    public string? Icon { get; init; }
    public double? Progress { get; init; }
}

public class ToolExecutionMeta
{
    public long DurationMs { get; init; }
    public DateTime ExecutedAt { get; init; }
    public string Source { get; init; } = "native";
    public string? Language { get; init; }
}
```

### Integration Points

**1. ToolExecutionService (line 134):**
```csharp
// Before:
result = new ToolResult(status, content);

// After:
var structured = StructuredToolResult.FromReactive(reactiveResult, toolCall.ToolName);
result = new ToolResult(status, structured.ToJsonString());
```

**2. Lua API (already done ✅):**
```csharp
// LuaApiTools.cs already returns structured table:
resultTable["content"] = content;
resultTable["success"] = DynValue.NewBoolean(success);
resultTable["tool"] = ToolToTable(tool, script);
resultTable["status_title"] = reactiveResult.StatusTitle;
resultTable["status_icon"] = reactiveResult.StatusIcon?.ToString();
```

**3. MCP Server:**
```csharp
// Return both text and structured metadata
return new CallToolResponse
{
    Content = [
        new TextContentBlock(reactiveResult.ResultContent),
        new ResourceContentBlock
        {
            Resource = new JsonResourceContents
            {
                Text = StructuredResult.ToJsonString(),
                MimeType = "application/json"
            }
        }
    ],
    IsError = !success
};
```

### Backward Compatibility

- The LLM (RCLLM) receives a JSON string as result content
- Old tools that return plain strings still work — they'll just have less metadata
- The structured format is **backward compatible**: if the LLM treats the result as a plain string, the "content" field is at the top level
- Clients that don't understand structured format can fall back to `content`

## Benefits

- **✅ Parseable by Lua, MCP, and RCLLM**
- **✅ Richer context for LLM** — knows status, icons, success/failure
- **✅ Easier debugging** — has execution time, source, metadata
- **✅ Future-proof** — can add fields without breaking existing code
- **✅ Consistent** — same format everywhere
