# Chat Management: Rename, Delete, Auto-Naming & Categorization

> **Status:** Feature Idea  
> **Priority:** High  
> **Tags:** chat, ux, organization, agents

## Problem

Currently chats have no proper management:
- No way to **rename** a chat
- No way to **delete** a chat
- No **auto-naming** — chats appear as "Chat #1", "Chat #2"
- No **categories/tags** — impossible to organize chats by topic
- No **description** — no context about what the chat is about

## Proposed Solution

### 1. Basic Chat Operations

- **Rename** — inline editing of chat title (double-click on name)
- **Delete** — with confirmation dialog ("Delete chat X? This cannot be undone.")
- **Context menu** — right-click on chat in sidebar → Rename / Delete / Duplicate

### 2. Auto-Naming Agent

An **auto-naming agent** that runs after the first few messages (or on demand) to:

- Analyze the conversation
- Generate a concise title (≤60 chars)
- Assign a **category/theme** from predefined list or free-form

```csharp
public class ChatAutoNamingService
{
    public Task<ChatNameSuggestion> SuggestNameAsync(Chat chat, CancellationToken ct);
}

public record ChatNameSuggestion
{
    public string Title { get; init; }
    public string? Category { get; init; }
    public string? Description { get; init; }
    public List<string> Tags { get; init; }
}
```

### 3. Category/Tag System

Predefined categories (editable):

| Category | Icon | Example |
|----------|------|---------|
| `coding` | 💻 | Writing code, debugging, architecture |
| `dnd` | 🐉 | Dungeons & Dragons sessions |
| `quest` | ⚔️ | Game quests, walkthroughs |
| `casual` | 💬 | General conversation |
| `politics` | 🏛️ | Political discussions |
| `roleplay` | 🎭 | Roleplay scenarios |
| `research` | 🔬 | Research & learning |
| `writing` | ✍️ | Creative writing, editing |

### 4. Theme Graph

A **category flow graph** that shows how conversations evolve:

```
Coding ──→ Research ──→ Casual
  │                      │
  └──→ Politics ←────────┘
```

- Automatically tracked when auto-naming runs
- Visualized as a directed graph
- Shows topic drift over time
- Clickable to filter chats by theme path

### 5. UI Changes

- Chat list item shows: **Icon + Title + Category badge + Last message preview**
- Category filter at the top of the chat list
- Search by title, category, or tags
- Bulk operations (select multiple → delete/categorize)

## Implementation Notes

- Auto-naming runs as a background agent (similar to summarizer)
- Category assignment can use existing agent/LLM infrastructure
- Theme graph stored in `ChatDatabase` alongside chat metadata
- UI should not block while auto-naming is in progress

## Priority

**High** — basic chat management is expected by any chat application.
