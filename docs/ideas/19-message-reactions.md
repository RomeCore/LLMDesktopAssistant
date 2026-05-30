# Message Reactions

> **Status:** Feature Idea  
> **Priority:** Medium  
> **Tags:** ux, interactions, agents, fun

## Problem

There's no way to react to messages. Users can only reply with text. This limits:

- Quick feedback (like/dislike/laugh)
- Agent-to-agent communication via reactions
- Fun interactions (banana reactions, memes)
- Signal for auto-triggered responses

## Proposed Solution

### 1. Reaction System

Users (and agents) can place **emoji reactions** on any message.

**Default reactions:**
- 👍 Like
- 👎 Dislike
- 😂 Laugh
- 😮 Wow
- 😢 Sad
- 🍌 Banana (meme reaction)
- ❤️ Love
- 🔥 Fire

**Custom reactions:**
- Any Unicode emoji
- Custom text reactions (future)

### 2. Reaction Events

When a reaction is placed, **events** are fired:

```csharp
public record MessageReactionEvent
{
    public Guid MessageId { get; init; }
    public string Emoji { get; init; }
    public string? ReactorId { get; init; }  // User login or agent ID
    public bool IsAgent { get; init; }
}
```

**Agent-triggerable reactions:**
- Agents can react via `agent-reaction_set` tool
- Agents can react automatically based on content analysis
- Reaction triggers can be configured: "If user reacts with 🍌, agent responds with 'Why banana?!'"

### 3. Auto-Response on Reaction (Optional)

If enabled, a reaction can **trigger an automatic response**:

```
User: "I fixed the bug"
User: [🍌 reaction]
Assistant: "Эээ, ты зачем мне банан поставил??? 🤨"
Another Assistant: "Ахахахаха, ему банан! 🍌🍌🍌" [also places 🍌]
```

- Configurable per agent via `AgentExecutionConditions.ReactionTriggers`
- Can start a new round when specific reaction is detected

### 4. UI Changes

- **Hover on message** → show reaction bar (+ button)
- **Click reaction** → toggle on/off
- **Long-press / right-click** → pick from extended emoji picker
- **Reaction count** shown below message
- **Animated reactions** — emoji pop animation on placement
- **Reaction list popup** — shows who reacted with what

### 5. Data Model

```csharp
public class MessageReaction
{
    public Guid MessageId { get; init; }
    public string Emoji { get; init; }
    public string ReactorId { get; init; }
    public bool IsAgent { get; init; }
    public DateTime ReactedAt { get; init; }
}
```

Stored in `ChatDatabase` alongside messages.

## Integration with Existing System

- **AgentReadSettings** — add `Reactions` read permission
- **AgentExposureMode** — add `Reactions` exposure flag
- **ToolModule** — add `reaction-set` and `reaction-list` tools
- **QuickActionService** — reactions can trigger quick actions

## Priority

**Medium** — nice-to-have for engagement, but not critical.
