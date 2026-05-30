# Broken LLM Provider Caching

> **Status:** Bug  
> **Priority:** High  
> **Tags:** bug, caching, llm, stability

## Problem

When switching chat branches back and forth, switching between different chats, or restarting the application, the LLM provider cache **resets** unexpectedly. This causes:

- Context loss — the LLM "forgets" previous messages
- Inconsistent behavior — the same prompt can produce different results depending on cache state
- Increased token usage — because context has to be re-sent
- Confusing UX — messages appear out of order or duplicated

## Root Cause

The root cause is an **unstable system prompt** that gets sent to the model. The prompt construction depends on:

- Agent configuration (persona, specialization, sliders, components)
- Chat history (summaries, message filtering)
- Context expanders (planned `IPromptContextExpander`)

When the cache key is based on the prompt hash, any instability in prompt construction causes cache misses.

### Specific Issues Found

1. **`ChatServiceProvider`** — a new service provider is created per chat, but caching doesn't account for provider identity
2. **`PromptChatBuilder`** — the prompt assembly may produce different output for the same logical state due to:
   - Unstable ordering of components/components
   - DateTime.Now usage in prompt (if any)
   - Non-deterministic iteration over dictionaries
3. **Missing cache invalidation** — when switching branches, the cache should be invalidated for the old branch but kept for the new one

## What Needs to Be Done

### 1. Debug & Stabilize Prompt Assembly

- Add logging to `PromptChatBuilder.Build()` to capture exact prompt text
- Compare prompts when switching branches to identify differences
- Make prompt construction **deterministic** (stable ordering, no timestamps in cache keys)

### 2. Proper Cache Key Design

```csharp
public record PromptCacheKey
{
    public int ChatId { get; init; }
    public Guid AgentId { get; init; }
    public int BranchVersion { get; init; }
    public string HistoryHash { get; init; }  // Hash of visible messages
    public string ConfigHash { get; init; }   // Hash of agent config at time of call
}
```

### 3. Cache Invalidation Rules

- **On branch switch** — invalidate cache for old branch, keep for new one
- **On message edit/regeneration** — invalidate from that point forward
- **On agent config change** — invalidate all caches for that agent
- **On app restart** — invalidate all caches (or persist with TTL)

### 4. Testing

- Unit tests for prompt determinism
- Integration test: switch branches 10 times, verify cache hits
- Stress test: rapid branch switching with 5+ agents

## Priority

**High** — this affects core UX reliability. Users lose confidence when the assistant "forgets" context.
