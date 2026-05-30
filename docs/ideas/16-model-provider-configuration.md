# Model Provider Configuration

> **Status:** Problem  
> **Priority:** Critical  
> **Tags:** config, models, providers

## Problem

Model providers are currently **hardcoded**. The app supports only:

- **OpenRouter** — requires `OPENROUTER_API_KEY` env var
- **Deepseek** — requires `DEEPSEEK_API_KEY` env var
- **Ollama** — requires local Ollama instance

There's no way to:
- Add custom OpenAI-compatible endpoints (e.g., local vLLM, LM Studio, TGI)
- Configure providers via UI
- Store API keys securely (not in env vars)
- Set per-provider default models
- Configure authentication methods (API key, Bearer token, no auth)

## What Needs to Be Done

### 1. Provider Registry

```csharp
public class LlmProviderRegistry
{
    private readonly Dictionary<string, ILlmProvider> _providers = new();
    
    public void Register(ILlmProvider provider) { }
    public ILlmProvider? GetProvider(string name) { }
    public IEnumerable<ILlmProvider> ListProviders() { }
}

public interface ILlmProvider
{
    string Name { get; }
    string DisplayName { get; }
    Uri DefaultEndpoint { get; }
    LLModelDescriptor[] AvailableModels { get; }
    Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken ct);
}
```

### 2. Built-in Providers

| Provider | Type | Auth | Default Models |
|----------|------|------|----------------|
| **OpenAI** | OpenAI-compatible | API Key | gpt-4o, gpt-4o-mini, o3, o4-mini |
| **OpenRouter** | OpenAI-compatible | API Key | Any (routes to selected model) |
| **Deepseek** | OpenAI-compatible | API Key | deepseek-chat, deepseek-reasoner |
| **Ollama** | Ollama API | None | Any pulled model |
| **Custom** | OpenAI-compatible | API Key / None / Bearer | User-defined |

### 3. Provider Configuration UI

- **Add Provider** button
- Form fields: Name, Endpoint URL, API Key (password field), Default Model
- **Test Connection** button
- List of configured providers
- Ability to enable/disable providers per chat
- Default provider selection

### 4. Secure Storage

- API keys stored in encrypted storage (not plain env vars)
- Option to use OS keychain (Windows Credential Manager, macOS Keychain)
- Migration path from env vars to UI storage

### 5. Model Discovery

- Auto-detect available models from Ollama
- Auto-detect models from OpenAI-compatible endpoints (if supported)
- Allow manual model name entry
- Per-provider model blacklist/whitelist

## Integration Points

- `LLModelSelectorControl.axaml` — needs to show models grouped by provider
- `AgentGenerationSettings.Model` — needs to work with provider+model pairs
- `ChatSettings.Models` — provider selection per chat

## Priority

**Critical** — the app is unusable for anyone who doesn't use the exact three hardcoded providers.
