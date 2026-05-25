# Macros & Secrets Management

> **Status:** Proposal  
> **Priority:** Medium  
> **Goal:** Provide a macro system for dynamic argument injection and a secrets sanitizer to prevent API keys from leaking to the LLM.

## Macros

Macros are placeholders in tool arguments that get replaced with real values before execution.

### Syntax

```
%%MACRO_NAME%%
%%MACRO_TYPE:VALUE%%
```

### Built-in Macros

| Macro | Replaced with | Example Result |
|-------|---------------|----------------|
| `%%LAST_USER_MESSAGE%%` | Last user message text | `"What is the weather?"` |
| `%%CHAT_HISTORY%%` | Recent chat history (truncated) | `"[{'role':'user','content':'...'}]"` |
| `%%NOW%%` | Current time (ISO) | `"2026-05-26T00:00:00Z"` |
| `%%WORK_DIR%%` | Working directory | `"C:\\Users\\...\\project"` |
| `%%RANDOM_UUID%%` | Random GUID | `"550e8400-e29b-41d4-a716-446655440000"` |
| `%%RANDOM_INT%%` | Random integer 0-999999 | `"472839"` |
| `%%USERNAME%%` | Current OS username | `"Roman"` |
| `%%HOSTNAME%%` | Machine hostname | `"DESKTOP-..."` |
| `%%ENV:PATH%%` | Environment variable | `"C:\\Windows;..."` |
| `%%SECRET:OPENAI_API_KEY%%` | Secret from settings | `"sk-..."` |
| `%%SECRET:ANY_KEY%%` | Any configured secret | `"value"` |

### Implementation

```csharp
public class MacroResolver
{
    private static readonly Regex _macroRegex = 
        new(@"%%(\w+)(?::(.+?))?%%", RegexOptions.Compiled);
    
    private readonly IEnumerable<IMacroProvider> _providers;
    
    public JsonNode Expand(JsonNode args, ToolExecutionContext context)
    {
        return ExpandNode(args, context);
    }
    
    private JsonNode? ExpandNode(JsonNode? node, ToolExecutionContext context)
    {
        return node switch
        {
            JsonValue value when value.TryGetValue<string>(out var str) 
                => JsonValue.Create(ExpandString(str, context)),
            JsonObject obj => ExpandObject(obj, context),
            JsonArray arr => ExpandArray(arr, context),
            _ => node
        };
    }
    
    private string ExpandString(string str, ToolExecutionContext context)
    {
        return _macroRegex.Replace(str, match =>
        {
            var type = match.Groups[1].Value;
            var param = match.Groups[2].Success ? match.Groups[2].Value : null;
            
            return ResolveMacro(type, param, context) ?? match.Value;
        });
    }
}
```

### Macro Providers

```csharp
public interface IMacroProvider
{
    string? Resolve(string type, string? param, ToolExecutionContext context);
}

// Built-in providers:

public class ContextMacroProvider : IMacroProvider
{
    public string? Resolve(string type, string? param, ToolExecutionContext context)
    {
        return type switch
        {
            "LAST_USER_MESSAGE" => context.Chat.Messages
                .LastOrDefault(m => m is UserMessage)?.Content,
            "CHAT_HISTORY" => SerializeHistory(context.Chat.Messages),
            "WORK_DIR" => context.Chat.Settings.Environment.GetWorkingDirectory(),
            "NOW" => DateTime.UtcNow.ToString("o"),
            "RANDOM_UUID" => Guid.NewGuid().ToString(),
            "RANDOM_INT" => Random.Shared.Next(0, 1000000).ToString(),
            "USERNAME" => Environment.UserName,
            "HOSTNAME" => Environment.MachineName,
            _ => null
        };
    }
}

public class SecretMacroProvider : IMacroProvider
{
    private readonly SecretSettings _secrets;
    
    public string? Resolve(string type, string? param, ToolExecutionContext context)
    {
        if (type != "SECRET" || string.IsNullOrEmpty(param))
            return null;
        
        return _secrets.GetValue(param);
    }
}

public class EnvironmentMacroProvider : IMacroProvider
{
    public string? Resolve(string type, string? param, ToolExecutionContext context)
    {
        if (type != "ENV" || string.IsNullOrEmpty(param))
            return null;
        
        return Environment.GetEnvironmentVariable(param);
    }
}
```

### When Macros Are Expanded

In the **Tool Execution Pipeline** (step ③), after JSON repair, before execution:

```csharp
// ToolExecutionService.cs
toolCall.Arguments = macroResolver.Expand(toolCall.Arguments, context);
```

## Secrets Management

### Problem

When `%%SECRET:OPENAI_API_KEY%%` is expanded to `sk-123456...`, that value is passed to the tool AND returned to the LLM in the result. The LLM could then read the API key.

### Solution: Secret Sanitizer

After tool execution, before the result goes back to the LLM, replace known secrets with `***`.

```csharp
public class SecretSanitizer
{
    private readonly HashSet<string> _secretValues;
    
    public SecretSanitizer(SecretSettings secrets)
    {
        _secretValues = new(secrets.GetAllValues(), StringComparer.Ordinal);
    }
    
    /// <summary>
    /// Sanitizes a result string by replacing known secret values with asterisks.
    /// </summary>
    public string Sanitize(string content)
    {
        foreach (var secret in _secretValues)
        {
            content = content.Replace(secret, "***");
        }
        return content;
    }
}
```

### Integration

```csharp
// ToolExecutionService.cs, after execution:
var sanitizedContent = secretSanitizer.Sanitize(reactiveResult.ResultContent);
toolCall.ResultContent = sanitizedContent;
```

### Secret Tracking

During macro expansion, track which secrets were injected:

```csharp
public class SecretTracker
{
    public List<string> InjectedSecrets { get; } = new();
    
    public string TrackAndExpand(string macro, string value)
    {
        InjectedSecrets.Add(value);
        return value;
    }
}

// Then sanitizer only needs to check injected secrets:
var sanitizer = new SecretSanitizer(tracker.InjectedSecrets);
```

## Configuration

### Storing Secrets

Secrets are stored in app settings:

```json
{
  "secrets": {
    "OPENAI_API_KEY": "sk-...",
    "GITHUB_TOKEN": "ghp_...",
    "DATABASE_PASSWORD": "pass123"
  }
}
```

Secrets can be managed via:
- Settings UI (password fields, masked)
- Environment variables
- `.env` file
- System credential manager (Windows Credential Manager, macOS Keychain)

### User Control

- Users can enable/disable specific macros
- Users can see which macros are available
- Users get warned when a tool uses secrets
- Secret values are never shown in UI or logs

## Benefits

- **✅ Dynamic arguments** — tools can reference chat context, environment, secrets
- **✅ Secure by default** — secrets are sanitized before reaching LLM
- **✅ Extensible** — new macro providers can be added easily
- **✅ Transparent** — users see which macros are used
- **✅ No breaking changes** — tools that don't use macros work as before
