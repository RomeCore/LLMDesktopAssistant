# Agent Long-Term Memory: Memory Blocks

> **Status:** Feature Idea (concept)  
> **Priority:** High  
> **Tags:** memory, agents, storage, settings, ltm

## Problem

The agent forgets everything between sessions. Auto-summarization (`ChatSummarizationService`) only compresses the current conversation — it is "working memory" management, not long-term memory. Knowledge about the user, their projects, preferences and past decisions evaporates when the chat ends or the context is compacted.

Long-term memory should be:
- **Persistent** — outlives any chat session
- **Reusable** — the same memory can be attached to multiple chats
- **Configurable** — per-user choice of what to remember and how
- **Opt-in** — disabled by default, no data collected unless the user attaches a memory block

## Proposed Solution: Memory Blocks (cartridges)

A **MemoryBlock** is simultaneously a *settings profile* and a *database*:

```
MemoryBlock (cartridge)
├── Configuration (embedding model, extraction rules, retrieval limits, importance decay)
└── Data (fact records + vector index, stored inside the block)

Chat ──attaches──▶ MemoryBlock A (user preferences)
                └─▶ MemoryBlock B (project X)
```

- **Many-to-many**: one block can be attached to many chats, one chat can have many blocks
- **Detaching ≠ deleting**: removing a block from a chat just hides its data; the block with all data stays and can be re-attached elsewhere
- **Deleting a block** physically removes its data (explicit destructive action)
- **Default state**: no blocks attached → memory disabled

### MemoryBlock as settings object

`MemoryBlock : SettingsObject` — a settings profile like `ChatSettings`, managed through the existing `SettingsCategory<TSettings>` / `SettingsManager` system:

- Lives in its own settings category (create/rename/copy/remove profiles via `SettingsCategoryView`)
- `ChatSettings` (or `Chat` itself) holds a list of attached block IDs
- Memory is disabled when the list is empty

### Data model (per-block record)

```
Fact: "Project X uses PostgreSQL"
├── Block: "project X"
├── Source: chat #42, message #1337, timestamp
├── Importance: 0.8 (decays over time / grows with usage)
├── Status: active | superseded | deleted (soft-delete, nothing is erased permanently)
├── Access count: 7
└── Vector: [0.12, -0.45, ...] (embedding of the fact text)
```

Storage layout:
- **LiteDB** (or per-block file store) — source of truth for records: fact text, metadata, scope, status
- **SemanticSector<Guid>** (already in RCLLM) — vector index only: record ID → embedding; search returns IDs, then records are loaded and filtered (scope/status)

## Memory lifecycle

### 1. Write

Two parallel mechanisms:

**A. Automatic extraction (background).** After each dialogue round (or on a token threshold trigger), the system takes the fresh slice of conversation and runs it through an LLM with a `memory_extraction_prompt` (LLT template, analogous to `summarization_prompt`). The model returns structured operations:

- `ADD` — new fact worth remembering
- `UPDATE` — old fact is outdated → old one marked `superseded`
- `DELETE` — fact no longer relevant
- `NOOP` — nothing to remember

The system decides which block to write into based on chat context (the chat is attached to "project X" → project facts go there).

**B. Agent-initiated write (tool).** The agent can explicitly call `memory_store(block: "...", fact: "...")` — e.g. when the user says "remember that..." or the agent makes an important conclusion. Agent writes into a **specific block**.

### 2. Store

Records live **inside the block** — autonomous, portable, survive chat deletion.

### 3. Retrieve

The agent calls `memory_search(query: "...", blocks: ["project X"])` — filtered by one/multiple blocks, or all attached blocks. Retrieval pipeline:

1. **Query transformation (HyDE)** — LLM generates a hypothetical answer; even a wrong one is "answer-shaped" and lands closer to stored facts in embedding space
2. **Hybrid search** — vector similarity (SemanticSector) + exact keyword matching (BM25-style / substring) to catch proper nouns like "PostgreSQL" or "auth.ts"
3. **Filtering** — drop `superseded`/`deleted`, account for importance and freshness, remove near-duplicates (MMR-style diversity)
4. **Return** — 3–5 best facts injected into agent context (more context ≠ better)

**Timing is agent-controlled**: memory is not auto-injected RAG on every request. The agent decides when to search; if nothing found it may search again with a different query (iterative retrieval for multi-hop questions).

### 4. Maintenance

- **Conflict detection at write time**: before storing, check cosine similarity against existing facts — `0.6–0.9` band = "similar topic, different fact" → surface the conflict instead of silently overwriting
- **Consolidation**: merge similar facts, collapse duplicates (periodic or trigger-based task)
- **Forgetting**: importance decays over time (Ebbinghaus-inspired) and with low access frequency; when the block overflows, low-importance facts are moved to `superseded`

## Integration with existing system

- **`MemoryBlock : SettingsObject`** — new settings category; profile UI comes free via `SettingsCategoryView`
- **`ChatSettings`** — add `AttachedMemoryBlocks: List<string>` (block IDs); empty = memory disabled
- **Extraction service** — `MemoryExtractionService` analogous to `ChatSummarizationService`; runs as an agent task after each round; LLT template `memory_extraction_prompt`
- **Tools** — new `ToolModule`: `memory_search`, `memory_store`, `memory_forget`, `memory_list_blocks` (agent sees attached blocks and their descriptions)
- **Embeddings** — already implemented in RCLLM (`CreateEmbeddingAsync` + Ollama/OpenAI-compatible clients); `SemanticDatabase`/`SemanticSector<T>` already exist and are not yet used by the app
- **Settings** — embedding model picker, extraction thresholds, retrieval limits, decay rates; inherited settings pattern via source generator if needed

## Open questions

- Should the agent see all attached blocks at once or operate on a specific one? (Concept: writes to a specific block, reads with optional filter)
- Block visibility/access permissions per agent (`AgentReadPermissions`-style)?
- Per-user blocks in multi-user Blazor web UI?
- Block descriptions shown to the agent to help it choose the right block?

## Priority

**High** — this is the missing layer between working memory (summarization) and truly persistent assistant behavior.
