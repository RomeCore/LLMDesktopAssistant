# Multi-Agent System

> **Part of dASS (Desktop Assistant) documentation**  
> Covers agent architecture, orchestration, privacy model, and execution stages.

---

## 1. Overview

dASS uses a **chat-centric multi-agent architecture**. Unlike systems where agents are isolated processes, here the **Chat is the first-class citizen** — agents share a common conversation history, respect each other's privacy settings, and are orchestrated through configurable execution stages.

```
┌─────────────────────────────────────────────────────────┐
│                         Chat                            │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌────────┐   │
│  │  User    │  │ Agent A  │  │ Agent B  │  │  ...   │   │
│  │ Messages │  │Messages  │  │Messages  │  │        │   │
│  └──────────┘  └──────────┘  └──────────┘  └────────┘   │
│                                                         │
│  ┌──────────────────────────────────────────────────┐   │
│  │           Execution Stages Pipeline              │   │
│  │  Stage 1 (Sequential) → Stage 2 (Adaptive) → ... │   │
│  │  (determines who speaks and when)                │   │
│  └──────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
```

---

## 2. Agent Model

### 2.1 `AgentDescriptor` — The Complete Agent Configuration

```csharp
public class AgentDescriptor : NotifyPropertyChanged
{
    Guid Id;                                      // Unique identifier
    AgentInformation Info;                         // Name, description, avatar
    AgentExecutionConditionsSettings ExecutionConditions; // When agent can be invoked
    AgentGenerationSettings Generation;            // Model, temperature, reasoning
    AgentReadSettings Read;                        // What agent can see and expose
    AgentPromptSettings Prompts;                   // System prompt, persona, sliders
    AgentToolSettings Tools;                       // Enabled tools, auto-approval
}
```

### 2.2 `AgentInformation` — Basic Identity

```csharp
public class AgentInformation
{
    string Name;                    // "Code Reviewer"
    string Description;             // "Specializes in code review and best practices"
    string Base64ProfileImage;      // Avatar image (base64 encoded)
}
```

### 2.3 `AgentPromptSettings` — How the Agent Speaks

| Property | Description |
|---|---|
| `SystemPrompt` | Custom system prompt override (optional) |
| `Nickname` | Alternate name the agent calls itself |
| `PromptComponents` | IDs of reusable prompt blocks (from `PromptRegistry`) |
| `UseCustomPersona` / `PersonaId` / `CustomPersona` | Personality template ("you are an expert in...") |
| `UseCustomSpecialization` / `SpecializationId` / `CustomSpecialization` | Knowledge domain specialization |
| `SliderValues` | Behavior slider settings (creativity, formality, conciseness) |

### 2.4 `AgentGenerationSettings` — How the Agent Generates

| Property | Description |
|---|---|
| `EnableCustomModel` | Override the default LLM model for this agent |
| `Model` | The specific model descriptor |
| `EnableReasoningSettings` / `ReasoningSettings` | Reasoning effort (Disabled → Maximum) |
| `EnableTemperature` / `Temperature` | Temperature override (0.0–2.0) |
| `EnableMaxTokens` / `MaxTokens` | Max tokens override |
| `AdditionalParameters` | Key-value pairs for provider-specific params |

### 2.5 `AgentToolSettings` — Tools Available

| Property | Description |
|---|---|
| `EnableTools` | Master switch for all tools |
| `AutoApproveLevel` | Max `ToolDangerLevel` that auto-approves (`Default`, `Safe`, `Warning`, `Dangerous`) |
| `ToolChanges` | Per-tool overrides (enable/disable, askForConfirmation) |

### 2.6 `AgentExecutionConditionsSettings` — When the Agent Can Be Invoked

```csharp
public class AgentExecutionConditionsSettings
{
    bool CanBeMentioned;      // Can @mentions invoke this agent
    bool CanMentionOthers;    // Can this agent @mention other agents
}
```

---

## 3. Privacy Model — Two-Way Visibility

This is the **core differentiator** of dASS's multi-agent system. Both the **sender** (what they expose) and the **receiver** (what they can read) must grant permission.

### 3.1 `AgentReadPermissions` — What an Agent Can Read

```csharp
[Flags] public enum AgentReadPermissions
{
    None                    = 0,
    UserMessages            = 1 << 0,  // Read user messages
    UserAttachments         = 1 << 1,  // Read user file attachments
    OwnMessages             = 1 << 2,  // Read own messages
    OtherAgentMessages      = 1 << 3,  // Read messages from other agents
    OtherAgentReasoning     = 1 << 4,  // Read reasoning/thoughts of other agents
    OtherAgentContent       = 1 << 5,  // Read content of other agents
    OtherAgentToolCalls     = 1 << 6,  // Read tool calls and results of other agents
    OtherAgentAttachments   = 1 << 7,  // Read attachments from other agents
    MessagesWithToolCalls   = 1 << 8,  // Read messages containing tool calls
    IdentifyAgentsAsUsers   = 1 << 9,  // See agents that identify as users
}
```

### 3.2 `AgentExposureMode` — What an Agent Exposes to Others

```csharp
[Flags] public enum AgentExposureMode
{
    None                    = 0,
    Reasoning               = 1 << 0,  // Expose reasoning/thoughts
    Content                 = 1 << 1,  // Expose main content
    Attachments             = 1 << 2,  // Expose attachments
    ToolCalls               = 1 << 3,  // Expose tool calls and results
    MessagesWithToolCalls   = 1 << 4,  // Expose messages containing tool calls
    IdentifySelfAsUser      = 1 << 5,  // Appear as a user to other agents
}
```

### 3.3 Visibility Rules

**Both** the sender's exposure AND the receiver's permissions must match:

```
Can agent B see agent A's tool calls?
→ Agent A must have AgentExposureMode.ToolCalls
→ Agent B must have AgentReadPermissions.OtherAgentToolCalls
→ AND both must agree on MessagesWithToolCalls if the message contains tool calls
```

### 3.4 Additional Filters

- **`AgentIdsReadFilter`** — White list or black list of agent IDs
- **`IsFilterWhiteList`** — `true` = only listed agents, `false` = all except listed
- **`MaxVisibleRounds`** — Limit how many recent conversation rounds are visible (0 = unlimited)
- **`AllowContextShields`** — Stop reading when a `ContextShieldViewModel` is encountered
- **`AllowSummaries`** — Allow seeing summarized history instead of full messages

### 3.5 Masking as User

When `IdentifySelfAsUser` is set on the sender (or `IdentifyAgentsAsUsers` on the receiver), the agent's message is presented as a regular user message — **reasoning, tool calls, and agent identity are hidden**.

---

## 4. Agent Storage: Global vs Local

| Type | Stored In | Scope |
|---|---|---|
| **Global Agents** | `AgentsConfiguration` (app settings) | Available in all chats |
| **Local Agents** | `Chat.Settings.Agents.ChatAgents` | Only in the current chat |

`AgentManagementService.ListAgents()` merges both lists, with local agents taking priority.

---

## 5. Execution Stages — Orchestration

Agents are not invoked randomly — they go through **execution stages**. A stage defines:
- Which agents participate
- In what order they are selected
- How many times they can execute

### 5.1 Stage Hierarchy

```
AgentExecutionStage (abstract base)
│
└── RepeatableAgentExecutionStage
    │
    ├── SequentialAgentExecutionStage
    │   — Agents execute one by one in order
    │
    ├── RandomAgentExecutionStage
    │   — Random selection with weighted probability
    │
    ├── MentionOnlyAgentExecutionStage
    │   — Only responds to @mentions, never auto-selects
    │
    ├── MentionableAgentExecutionStage (abstract)
    │   — Base for stages that support @mentions
    │   ├── RandomAgentExecutionStage (inherits)
    │   ├── SequentialAgentExecutionStage (inherits)
    │   └── AdaptiveAgentExecutionStage
    │       — Uses an LLM router to decide the next agent
    │
    └── AdaptiveAgentExecutionStage
        — LLM-based router with context awareness
```

### 5.2 Repeatable Properties

Properties inherited from `RepeatableAgentExecutionStage`:

| Property | Description |
|---|---|
| `CanAgentsExecuteAgain` | Can the same agent be selected multiple times in this stage |
| `MinIterations` | Minimum executions before stage can stop |
| `MaxIterations` | Maximum executions before stage must stop |
| `StopChance` | Probability (0.0–1.0) of stopping on each iteration after MinIterations |

### 5.3 Stage Types in Detail

#### SequentialAgentExecutionStage
Agents execute in the order they appear in the list. Each agent executes once per pass. When all have executed, the stage passes control to the next stage (or stops).

#### RandomAgentExecutionStage
Selects an agent randomly, weighted by each agent's `Weight` property:
```csharp
// Agent with Weight = 2.0 is twice as likely to be chosen
// as an agent with Weight = 1.0
```

Also checks for `@mentions` before falling back to random selection.

#### MentionOnlyAgentExecutionStage
Does not auto-select any agent. Only responds when another agent or user types `@AgentName` in their message. The `@mentions` are detected via regex:
```csharp
var regex = new Regex(string.Join("|", agentNames.Select(a => $"@{Regex.Escape(a)}\\b")));
```

#### AdaptiveAgentExecutionStage
The most advanced stage — uses a **separate LLM model (router)** to decide the next agent:

1. First checks for `@mentions` (inherits from `MentionableAgentExecutionStage`)
2. If no mention found, calls the router LLM with:
   - List of agents (name + description)
   - Context of the last `MaxVisibleRounds` rounds
   - Additional router prompt (if specified)
   - `EnforceRouterSelection` flag
3. Router responds with `@AgentName` or `None`
4. If `EnforceRouterSelection` is true and router returns nothing → falls back to random weighted selection

The router prompt template lives in `Prompting/Resources/router.llt` and includes:
- System instructions for agent selection
- Examples of correct routing decisions
- The conversation context and agent list

### 5.4 Execution Pipeline

`AgentOrderingService.GetNextAgentAsync()` orchestrates the flow:

```
1. Collect the current round (all messages since last user message)
2. Identify the previous agent and stage
3. Determine the target stage index
4. Iterate through stages in order:
   for each stage:
     call stage.GetNextAgentAsync(context)
     if it returns an agent → return (agentId, stageId)
5. If all stages return null → wait for user input
```

### 5.5 Round Detection

A "round" starts with a user message and includes all subsequent assistant messages:

```
[User]  "Write a function"  ← round starts
[AgentA] "Here's the design"
[AgentB] "Here's the code"
[User]  "Thanks"            ← new round starts
```

---

## 6. Agent Execution Flow

When an agent is selected, `ChatExecutionService.GenerateResponseWithAgentAsync()`:

1. **Build LLM** — creates the appropriate model client (respecting agent's GenerationSettings)
2. **Build Prompt** — `PromptChatBuilder.Build(agent)`:
   - Filters messages by permissions (read + expose)
   - Renders system prompt with components, persona, sliders
   - Converts messages to LLM-native format
3. **Build Toolset** — `ToolsetBuildingService.BuildTools(agentId)`:
   - Merges all tool sources
   - Applies per-tool overrides
4. **Execute Loop**:
   ```
   Send messages to LLM → Receive response
   If response has tool calls → Execute tools → Append results → Repeat
   If response has text → Append to chat → Return
   ```
5. **After execution**:
   - If the last tool call mentioned another agent → `AgentOrderingService` may switch agents
   - Otherwise → wait for user or next stage trigger

---

## 7. @Mention System

Any agent can mention another by typing `@AgentName` in its response:

```markdown
I've designed the architecture. @Coder, please implement this.
```

The mention system:
- Checks if the mentioning agent has `CanMentionOthers`
- Checks if the mentioned agent has `CanBeMentioned`
- Uses regex matching against all known agent names
- Detection happens in `MentionableAgentExecutionStage`

---

## 8. Agent Instance Weight

Each agent in a stage has an `AgentInstance`:

```csharp
public class AgentInstance
{
    Guid AgentId;
    bool Enabled;
    double Weight;        // Selection weight (for random stages)
}
```

Weights allow probabilistic load balancing between agents with similar capabilities.

---

## 9. Architecture Diagram

```
                      ┌─────────────────┐
                      │  User Message   │
                      └────────┬────────┘
                               │
                               ▼
                    ┌──────────────────────┐
                    │ AgentOrderingService │
                    │  (per-chat service)  │
                    └──────────┬───────────┘
                               │
                   ┌───────────┴───────────────┐
                   │  Execution Stages         │
                   │  ┌─────────────────────┐  │
                   │  │ Stage 1: Sequential │  │
                   │  │   AgentA → AgentB   │  │
                   │  └─────────────────────┘  │
                   │  ┌─────────────────────┐  │
                   │  │ Stage 2: Adaptive   │  │
                   │  │  Router LLM decides │  │
                   │  └─────────────────────┘  │
                   │  ┌─────────────────────┐  │
                   │  │ Stage 3: MentionOnly│  │
                   │  │  @mentions only     │  │
                   │  └─────────────────────┘  │
                   └───────────┬───────────────┘
                               │
                               ▼
                    ┌─────────────────────┐
                    │ChatExecutionService │
                    │  Build LLM          │
                    │  Build Prompt       │
                    │  Build Toolset      │
                    │  Execute Loop       │
                    └─────────────────────┘
                               │
                 ┌─────────────┴─────────────┐
                 │                           │
                 ▼                           ▼
        ┌─────────────────┐       ┌─────────────────┐
        │  Agent responds │       │  Tool calls     │
        │  with text      │       │  → Execute      │
        └─────────────────┘       └─────────────────┘
                 │                           │
                 └─────────────┬─────────────┘
                               │
                               ▼
                    ┌─────────────────────┐
                    │  Next iteration or  │
                    │  wait for user      │
                    └─────────────────────┘
```

---

## 10. Best Practices

1. **Define clear agent purposes** — give descriptive names and descriptions for the router
2. **Use `@mentions` for explicit handoffs** — more reliable than relying on the router
3. **Set `MaxVisibleRounds`** — prevents context overflow for specialized agents
4. **Use `ContextShields`** for sensitive information that should not persist
5. **Balance `AutoApproveLevel`** — too permissive risks safety, too strict disrupts flow
6. **Use `Weight` for load balancing** between similar agents
7. **Combine stage types** — e.g., Sequential → Adaptive → MentionOnly for complex workflows

---
