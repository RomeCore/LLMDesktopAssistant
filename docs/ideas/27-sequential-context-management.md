# SCM — Sequential Context Management (v0 Specification)

> **Status:** Design Specification (v0 draft — assembled from design discussion)
> **Priority:** High (core prompting infrastructure)
> **Tags:** prompting, context, caching, agents, architecture, llm

---

## 0. TL;DR

SCM keeps the LLM prompt **byte-stable** and communicates **state changes as events** in the message
stream. Instead of rebuilding the system prompt (and killing the provider prefix cache) every time a
tool appears, a skill is disabled, or a persona is tweaked, the system:

1. **Freezes** the prompt-affecting state into a snapshot ("frozen baseline"),
2. Delivers **deltas ("announcements")** as message-attached objects when the state changes,
3. **Rebaselines** when the context is cut (checkpoints, summarization, compaction) so the model
   never silently loses or misreads the state.

Three prompt modes: **dynamic** (current behavior), **static** (fully frozen, manual refresh),
**hybrid SCM** (frozen baseline + deltas + checkpoint-aware rebaseline). The system is per-agent,
section-oriented, and append-only: history is never rewritten, dead objects are suppressed at
build time, not deleted.

---

## 1. Problem

Two independent, compounding failures motivate SCM:

1. **Cache death.** Any change to state that affects the system prompt (tools, Lua scripts, skills,
   sub-agents, persona, prompt components, memory blocks, timezone, …) forces a prompt rebuild.
   For providers with prefix caching this invalidates everything after the change point; for local
   models it discards precomputed KV state. On long-lived chats this is paid on every turn.
2. **Invisibility.** Models do not "notice" silent system prompt mutations. There is no event, no
   timestamp, no narrative — the model cannot say *when* something changed or react to the change.
   A change buried in a 20K-token prompt is indistinguishable from background.
3. **Checkpoint fragility.** Context shields and summarization/compaction sever the effective
   history. A naive "change log" of deltas loses its baseline: the model sees diffs without the
   snapshot they refer to, and knowledge silently drifts.

Claude Code fights the same problems with per-surface patches (deferred tools delta, agent listing
delta, MCP instructions delta, skill listing suppression, `date_change`). Those patches validate the
need — and their production numbers are brutal (see §15). SCM generalizes the approach into a single
protocol.

## 2. Goals / Non-goals

**Goals (v0):**

- Byte-stable prompt prefix between prefix-rewrite events; announcements are tail-only.
- State changes become explicit, timestamped events in the conversation ("biography").
- Checkpoint-aware resynchronization: rebaseline after any effective-history cut.
- Per-agent scoping (each agent's effective addon set/prompt is different).
- Universal section model: any addon pack or subsystem can register a tracked section.
- Three prompt modes (dynamic / static / hybrid SCM), selectable via settings inheritance.

**Non-goals (v0):**

- Message reactions — separate system (see `docs/ideas/19-message-reactions.md`).
- Summarization mechanics themselves (SCM only *reacts* to summary events).
- Memory retrieval (`AutomaticMemoryReader`) — unchanged.
- Sub-agent / agentic-task prompting integration — v1+.
- Cross-chat shared state — v1+.

## 3. Core principles

1. **Clock-in-events.** Time belongs to messages, not to the prompt. The frozen prompt contains no
   "now". Message timestamps carry the timeline; timezone is a tracked *section* (not a clock).
2. **Rendered-bytes freeze.** Anything rendered into the prompt is rendered **once, at creation**,
   and never re-rendered at injection time (no `DateTime.Now`, no relative dates, no recomputed
   headers). Stored bytes are injected as-is.
3. **Append-only history.** Emitted objects are never rewritten or deleted when outdated. Validity
   is evaluated at build time and stale objects are suppressed, not mutated.
4. **Events over swaps.** Changes are communicated as events in sequence, not as silent prompt
   mutations. The model must be able to reference *what* changed and *when*.
5. **Exact-prefix caching.** Between prefix-rewrite events the prefix is byte-identical. Everything
   new is appended at the tail (invisible data messages).
6. **Per-agent scope.** Deltas and anchors are computed and delivered per agent
   (`AttachedMessageMode.AgentPrivate` semantics). No agent sees another agent's announcements.
7. **Self-describing chain.** Objects carry `{EpochId, Seq, PrevSeq, StateAfter}` — the chain
   validates itself by scanning surviving objects. No external mutable ledger is the source of
   truth (a tiny chat-level pointer is allowed for diagnostics/fast paths only).
8. **Learn from production.** Borrow proven patterns from Claude Code: announcement wording,
   carry vs re-announce policies, "no retroactive retractions", reconstruction diagnostics.

## 4. Terminology

| Term | Definition |
|---|---|
| **Section** | A tracked unit of prompt-affecting state: tools, skills, sub-agents, Lua scripts, persona, components, specialization, timezone, memory blocks, … |
| **Frozen baseline / snapshot** | Byte-stable rendering of the system prompt + tool array + per-section states, produced at freeze time. |
| **Anchor** | Message-attached object marking the start of an epoch. Holds epoch id, reason, and the state snapshot the epoch is chained to. |
| **Delta / announcement** | Message-attached object carrying changes vs. the last known state: epoch id, `Seq`, `PrevSeq`, rendered text, `StateAfter`. |
| **Epoch** | Interval started by an anchor. Deltas belong to the epoch of the anchor they chain to. |
| **Rebaseline** | Emitting a fresh anchor (full current drift snapshot) when the chain is broken. |
| **Prefix rewrite event** | Anything that mutates the effective prefix: checkpoint (context shield) set, summarization completed, summary edited, tool-call compaction, branch switch, history edit/delete affecting the prefix, manual refresh, anchor loss. |
| **Effective history** | The per-agent message set that will actually be sent (after visibility filters, shield/summary cuts; and round window in non-hybrid modes). |
| **Carry / re-announce / silent** | Per-section policy for what happens at rebaseline (state carried implicitly, re-announced, or deliberately not re-announced for token reasons). |

## 5. Prompt modes

| | Dynamic (current) | Static | Hybrid SCM |
|---|---|---|---|
| System prompt source | live build per request | snapshot stored in **agent config** | snapshot stored in **anchor objects** |
| Tool array source | live (`ValidTools`) | frozen | frozen |
| `MaxVisibleRounds` | active | active | **not active** — cuts only by events |
| Change tracking | none | none (manual refresh) | deltas + rebaseline |
| Cache behavior | prefix changes freely | stable until refresh | stable until prefix-rewrite events |
| Persistence | n/a | snapshot survives app restart | objects persist in message history |
| Deltas emitted | no | no | yes |

**Mode semantics:**

- **Dynamic** — status quo. SCM objects are not emitted and not injected; stale objects already in
  history are inert data. Window trimming behaves as today.
- **Static** — the rendered system prompt + tool array are frozen into agent config and updated only
  by an explicit button (with a live-vs-frozen **diff preview**). Survives restart. Calls to tools
  removed since freezing produce a graceful tool-error; tools added since freezing are not callable
  until refresh — this is a documented contract, not a bug.
- **Hybrid SCM** — per-agent anchor + deltas; the prompt and tool array are frozen from the anchor;
  no sliding window (semantic antithesis: silent amnesia vs. explicit forgetting as an event).
  Rebaseline on any prefix-rewrite event. Re-freeze optionally emits a **refresh-note** announcement
  ("baseline refreshed: reason; changes vs previous baseline: …").

**Mode selection:** an inheritable agent setting (`PromptMode`) using the existing settings
inheritance (`Application` / `Profile` / `Agent`). Default: **dynamic** in v0 (hybrid opt-in until
battle-tested).

**Mode switching:**

| Transition | Behavior |
|---|---|
| dynamic → hybrid | lazily create the first anchor at the next prep (reason: `ModeSwitch`) |
| hybrid → dynamic | stop emissions; existing objects remain as inert data |
| any → static | freeze current rendering into config (explicit user action + preview) |
| static → hybrid | create anchor from the current live rendering (reason: `ModeSwitch`) |

## 6. Data model

All objects inherit `AdditionalChatData` (`IsVisible = false`, `IsTemporary = false` → persisted via
`AdditionalChatDataSynchronizer`). Attachment rules: assistant messages only, `AgentPrivate` mode;
attached to the **pending response message** (`context.Response`) by the SCM stage.

```csharp
// Attached to a message. Marks the start of an epoch.
class ScmAnchor : AdditionalChatData
{
    Guid ChatId;
    Guid AgentId;                // per-agent scope
    long EpochId;                // monotonic per (chat, agent)
    int  SchemaVersion;
    DateTime CreatedAt;          // frozen bytes
    AnchorReason Reason;         // Initial | Checkpoint | ChainGap | ManualRefresh
                                 // | ModeSwitch | AgentJoin | SessionResume | SchemaMigration
    ScmStateSnapshot State;      // section states this epoch is chained to
    string? Content;             // usually null or terse; baseline lives in the frozen prompt
}

// Attached to a message. Carries a change announcement.
class ScmDelta : AdditionalChatData
{
    Guid ChatId; Guid AgentId;
    long EpochId;
    long Seq;                    // 1, 2, 3, ... within the epoch
    long PrevSeq;                // Seq-1 (self-describing chain; gap detection)
    int  SchemaVersion;
    DateTime CreatedAt;          // frozen bytes
    ScmSectionChanges Changes;   // machine-readable: added/removed/changed per section
    string Content;              // rendered announcement text, frozen at creation
    ScmStateSnapshot StateAfter; // state for computing the next delta without history walks
    bool IsInitial;              // first announcement in the epoch (affects header wording)
}

class ScmStateSnapshot { Dictionary<string, SectionState> Sections; }
class SectionState     { string SectionId; string ContentHash; /* refs to ids/values */ }

// Attached to the Chat (never injected into the prompt). Diagnostics / fast path only.
class ScmChatPointer : AdditionalChatData
{
    long CurrentEpochId;
    long LastSeq;
    Guid? AnchorMessageId;
    string? LastStateHash;
}
```

Notes:

- `Content` is the **model-facing** part; everything else is bookkeeping.
- `StateAfter` is what kills full-history walks: the next diff is computed against the last
  surviving object's state.
- The chain is validated by scanning **surviving** objects (§7); `PrevSeq` makes gaps detectable
  without any external registry.

## 7. Build-time algorithm (validation & emission)

SCM requires a small core refactor: `ChatPromptBuilder.Build` becomes **two-phase**:

1. **Walk / select** — compute the effective message set for the agent (visibility filters,
   shield/summary cuts, window if applicable) and the cut metadata.
2. **SCM stage** — runs between walk and conversion. It receives `(agent, effectiveMessages,
   pendingResponse)` and may attach objects to the pending response.
3. **Convert / inject** — normal message conversion + `AttachedMessageInjectionHook` picks up newly
   attached objects (they are already present before this phase).

At every prep for agent **A** (hybrid mode):

1. Find the **latest surviving anchor** for A in the effective set → active epoch.
2. Collect surviving deltas of that epoch; verify chain contiguity
   (`anchor.Seq = 0`; each delta's `PrevSeq == previous.Seq`).
3. Compute the current state from live collectors; determine target diff base
   (last surviving object's `StateAfter`, or anchor's `State`, or nothing).

**Decision table:**

| Active anchor alive | Chain contiguous | Changes vs. known state | Action |
|---|---|---|---|
| yes | yes | none | nothing |
| yes | yes | some | emit **delta** (`Seq = last + 1`, `PrevSeq = last`) |
| yes | no (gap) | any | **rebaseline** (reason `ChainGap`) |
| no | — | any | **rebaseline** (reason `Checkpoint` / `AnchorLost`) |
| no | — | none | **rebaseline** (keep the invariant; content may be terse) |

Implementation rules:

- Suppression of dead-epoch objects happens **at the stage** (they are simply not injected), never by
  mutating or deleting objects. No transient properties on persisted VMs — avoid `ChangeTracker`
  churn and DB writes per build.
- Emission always targets the pending response message; the object is part of history from that
  moment on (retries/resends re-send it naturally).
- Rebaseline renders the **full current drift vs. the frozen baseline**, in the same format as
  deltas, so the model always reads a consistent channel.

## 8. Sections & default policies (v0)

| Section | Scope | Change signal | Announcement | On rebaseline | Notes |
|---|---|---|---|---|---|
| **Addons** — tools / Lua scripts / skills / sub-agents | per agent | pool diff (added/removed) | bare names + one-liner; full info on demand via `addon-info` | re-announce names | hidden addons: **not announced**, still callable (open question, see §16) |
| **Persona / Specialization** | per agent | content change | replacement statement (full new text if small, summary if large) | re-announce (or carry via fresh baseline) | threshold policy TBD |
| **Prompt components** | per agent | content change | same as persona | same | |
| **Timezone** | per agent/chat | value change | `<old> → <new>` | carried by fresh baseline | timezone ≠ time; message timestamps carry the clock |
| **Memory blocks** (enabled list & permissions) | per agent | list change | names + access flags | re-announce names | |

Per-section policy axes: **granularity** (list vs. text), **render style**, **re-announce policy**
(`re-announce` / `carry` / `silent`, with token budget rationale), **removal semantics**.

## 9. Announcement rendering

Rules:

1. **Frozen at creation.** The `Content` string is rendered once and stored; injection never
   recomputes anything (no dates, no counts, no relative phrasing).
2. **Channel marker.** Announcements are wrapped in a recognizable block; the frozen system prompt
   contains a one-line pointer describing the channel, e.g.:

   > *"State changes (tools, skills, persona, timezone, etc.) may arrive as `<state_update>`
   > messages in the conversation; they are authoritative. Use `addon-info` / `addon-list_available`
   > for details."*

3. **Wording.**

```
<state_update kind="delta" epoch="12" seq="4" at="2026-09-20 14:02:11 (UTC+05:00)">
[tools added]   weather — get forecasts for a city (details: addon-info weather)
[tools removed] music — no longer available; calls will fail
[timezone]      UTC+05:00 → UTC+03:00
</state_update>
```

   Baseline variant: `kind="baseline"` (post-cut resync, full drift vs. frozen prompt).

4. **Initial vs. subsequent.** First announcement in an epoch uses a "listing" header
   ("Available agent types…"), later ones use an update header ("New agent types are now
   available…"). Mirrors Claude Code's `isInitial` pattern.
5. **No retroactive retractions.** Old announcements are never contradicted by editing; new
   announcements describe the new truth. A tool that becomes "undeferred but still available" is
   **silent**, not reported as removed.
6. **Removals are actionable.** Removal announcements say what will happen ("calls will fail"),
   so the model doesn't chase ghosts.
7. **Tool call resolution order** (execution side): live exact → frozen exact → alias →
   later-announced (not yet in a frozen set) → unknown. Unknown calls produce a self-documenting
   error: nearest-name suggestions, and the schema/usage hint if the name was announced.

## 10. Cache & prefix rules

1. **Memoized sections.** Frozen prompt sections are computed once per epoch; recomputation happens
   only on prefix-rewrite events. Any section that must recompute per turn requires an explicit
   marker + reason (Claude Code's `DANGEROUS_uncachedSystemPromptSection` pattern — adopt the
   convention and consider a lint rule).
2. **Prefix-rewrite events (canonical list):** checkpoint set · summarization completed · summary
   edited · tool-call compaction · branch switch · history edit/delete affecting the prefix ·
   anchor loss detected · manual refresh · mode switch · schema migration.
3. **Announcements are tail-only.** A delta MUST NOT change any byte before its insertion position.
   This is a testable invariant (§13).
4. **Batch the cuts.** Each prefix rewrite kills the cache for everything after it; prefer fewer,
   larger cuts over many small ones (e.g., batch tool-call compaction rather than doing it every
   round). In hybrid mode there is no sliding window; growth is controlled by event-driven cuts.
5. **Rendered-bytes freeze applies to everything**: message timestamps, memory-age headers, delta
   text. When changing any renderer format, treat it as a one-time prefix rewrite.

## 11. Core refactor requirements (dASS)

1. `ChatPromptBuilder.Build` — two-phase split (walk → SCM stage → convert/inject).
2. SCM stage interface — receives `(agent, effectiveMessages, pendingResponse)`; can attach VMs;
   exposes survivors for validation.
3. Prefix-rewrite event stream — a chat-scoped notifier (checkpoint set, summary completion/edit,
   compaction, branch switch, manual refresh) plus invariant detection inside the stage
   ("anchor not in survivors").
4. Request assembly — system prompt + tool array sources switch by mode: dynamic → live;
   static → agent-config snapshot; hybrid → anchor snapshot. (`ChatExecutionService` touchpoint.)
5. Tool resolution — extend `ToolsetCacheService` resolution per §9.7; graceful errors for removed
   tools; alias priority rules.
6. Settings — `PromptMode` as an inheritable agent setting; frozen snapshots storage for static
   mode.
7. Diagnostics — chain verification API; effective-set exposure for the inspector; prompt dump
   integration.

## 12. Diagnostics & tooling

- **SCM Inspector:** chain timeline per agent (anchors/deltas, epochs, `Seq`/`PrevSeq`, reasons),
  dead vs. live objects, frozen-vs-live diff, refresh preview, "what did the model know at message
  N" reconstruction.
- **Logging:** emission decisions, validation results, and call-site context (Claude Code's
  inc-4747 lesson: stateless reconstruction bugs are invisible without per-site diagnostics).
- **Golden tests:** announcement text snapshots; prefix-stability hashes.

## 13. System invariants (testable)

- **I1.** Between state changes, two consecutive builds for the same agent produce a byte-identical
  prompt prefix.
- **I2.** An announcement never alters bytes before its insertion point.
- **I3.** SCM objects are append-only; suppression is runtime-only (no mutation/deletion of stale
  objects).
- **I4.** For any surviving epoch chain, the chain is contiguous (`PrevSeq == prior Seq`) or a
  rebaseline occurs at the next prep.
- **I5.** Dead-epoch objects (no surviving anchor) are never injected.
- **I6.** Every emitted object's `Content` is byte-stable after creation (no re-render at injection).
- **I7.** Per-agent isolation: an agent never sees another agent's anchors/deltas.

## 14. Acceptance criteria (scenarios)

Each scenario is a test: *Setup* → *Expected behavior* → *Assertions*. Product-level behavior and
system-level invariants are both checked.

### AC-1. Night owl (time context)

**Setup:** user sends a message at 03:47 local time.
**Expected:** assistant can react with time-aware personality, e.g. *"Какого хуя ты не спишь в
3:47 ночи?!"* — the time comes from the message timestamp, not from a prompt clock.
**Assertions:** message carries frozen `time_sent`; timezone section is frozen and stable; no
prompt mutation occurred; prefix unchanged (I1).

### AC-2. Traveler (timezone delta)

**Setup:** user's timezone changes mid-chat (UTC+05:00 → UTC+03:00).
**Expected:** a delta announces `[timezone] UTC+05:00 → UTC+03:00`; the assistant adjusts
("Ты в Стамбуле, что ли? Пересчитываю напоминалки").
**Assertions:** delta attached per-agent; prefix bytes unchanged (I2); chain valid (I4); no
re-freeze — the frozen prompt keeps the old value, the delta is authoritative until the next
rebaseline.

### AC-3. Week away (return + rebaseline)

**Setup:** app closed for 5 days; plugins/MCP/skills changed meanwhile; user returns.
**Expected:** on the next prep the anchor is invalid → rebaseline; the assistant can greet with a
gap-aware summary ("Пять дней не виделись… кстати, пока тебя не было: +2 скилла, сервер X
отключился").
**Assertions:** rebaseline emitted with a correct reason; re-announce policy applied per section;
old objects remain in history, suppressed (I3, I5); assistant able to reference the gap via
timestamps.

### AC-4. New capability (tool added mid-chat)

**Setup:** user enables a new tool/skill (e.g., calendar) mid-conversation.
**Expected:** delta with a bare name + one-liner; the assistant uses it in the same round and may
offer it ("Опа, мне выдали календарь! Давай перенесу твои 'вечером сделаю'…").
**Assertions:** delta in the tail; prefix before insertion unchanged (I1, I2); the new tool is
callable immediately via the resolution order (§9.7); `IsInitial`/header wording correct.

### AC-5. Lost capability (tool removed mid-chat)

**Setup:** user disables a tool/skill.
**Expected:** removal announcement ("no longer available; calls will fail"); the assistant stops
using it and may comment ("Ты забрал у меня музыку?? Верни, сука").
**Assertions:** removal delta emitted; a direct call produces the graceful error; no hallucinated
usage; no retroactive retraction of the original "added" announcement.

### AC-6. Persona tweak

**Setup:** persona/components edited mid-chat (including co-editing with the assistant).
**Expected:** delta with the replacement statement; behavior visibly changes in the same round
("Чувствую себя как-то язвительнее. Дифф видел: вежливость −20%…").
**Assertions:** section diff correct; prefix unchanged; at the next rebaseline the state is carried
by the fresh baseline (no duplicate announcement needed).

### AC-7. Explain the change

**Setup:** user asks "почему ты вдруг стал отвечать короче?"
**Expected:** the assistant cites the change event with its timestamp ("в 12:47 ты включил режим
краткости").
**Assertions:** every announcement carries a frozen `at=` timestamp; inspector shows the timeline;
the model can ground its explanation in surviving objects.

### AC-8. Amnesia is an event (checkpoint / summary)

**Setup:** long chat gets summarized (or a context shield is set) mid-conversation.
**Expected:** a prefix-rewrite event; on the next prep the old anchor is not in survivors →
rebaseline; the assistant can acknowledge the operation ("я сжал нашу историю — детали подтёрлись,
но важное про X и Y помню").
**Assertions:** rebaseline reason `Checkpoint`/`Summary`; post-cut knowledge consistent (tools
still "known" via the fresh baseline); no silent state loss; old deltas suppressed (I5).

### AC-9. You promised (time quoting)

**Setup:** user promised something at 18:40; asks at 23:30.
**Expected:** the assistant can quote both times and hold the user accountable.
**Assertions:** timestamps frozen in messages (I6); no prompt clock involved; timeline consistent
across timezone deltas (AC-2 composition).

### AC-10. Team grows (sub-agent added, per-agent isolation)

**Setup:** user adds a sub-agent; the chat runs multiple agents; a second agent joins later.
**Expected:** delta announces the new sub-agent to its owning agent only; the later-joining agent
lazily gets its own anchor (reason `AgentJoin`) on its first prep.
**Assertions:** per-agent isolation (I7); no cross-agent leakage of announcements; the new agent's
baseline matches its own effective set.

### AC-11. Prefix stability (technical)

**Setup:** two consecutive preps with no state changes.
**Expected:** no new objects; prompt prefix byte-identical; no DB writes from SCM.
**Assertions:** hashes equal (I1, I6); no `ChangeTracker` churn.

### AC-12. Chain gap → rebaseline (technical)

**Setup:** user deletes or branches away a message carrying a delta, while the anchor survives.
**Expected:** `PrevSeq` mismatch detected during validation → rebaseline (reason `ChainGap`) even
though the anchor is alive.
**Assertions:** detection without full-history walks (uses surviving objects only); rebaseline in
the same prep; no mutations of existing objects (I3).

### AC-13. Static mode snapshot (technical)

**Setup:** agent switched to static; plugins update in the background; app restarts.
**Expected:** prompt/tool array stay exactly as frozen; background changes do not leak; refresh
button shows a diff and updates the snapshot; calls to removed tools yield graceful errors; added
tools are not callable until refresh (documented contract).
**Assertions:** snapshot persistence across restart; diff preview correctness; no hidden updates.

### AC-14. Mode switching (technical)

**Setup:** dynamic → hybrid → dynamic → static → hybrid.
**Expected:** per §5 switching table; anchors created lazily on entering hybrid; no duplicate
anchors; emissions stop/start cleanly.
**Assertions:** at most one active epoch per agent; stale objects inert; no errors on legacy data.

### AC-15. Removed tool + aliases (technical)

**Setup:** the model calls a removed tool, an aliased tool name, and an invented name
(`fs-read_file` vs `fs-read_entry`).
**Expected:** resolution order applies (live → frozen → alias → announced → unknown); errors are
self-documenting (nearest matches + schema hint when announced); no provider-side validation
assumptions are relied upon (smoke-tested per provider).
**Assertions:** correct resolution for all four cases; graceful failures; user-visible error text.

## 15. Prior art: Claude Code

Proven patterns borrowed (from production code):

- Persisted **delta attachments** in the message stream (`deferred_tools_delta`,
  `agent_listing_delta`, `mcp_instructions_delta`), rendered as `<system-reminder>` messages.
- Announced-set **reconstruction from history** (self-validating without an external store).
- **Re-announce vs. carry** policies after compaction, with explicit token budgets
  (e.g., skipping the ~4K-token skill listing re-announcement).
- Boundary **carry** of selected state (`preCompactDiscoveredTools`).
- **No retroactive retractions**; undeferred-but-available names are silent.
- **Rendered-byte stability** paranoia: precomputed memory-age headers; date changes appended at
  the tail while the prefix keeps a stale date; ~920K effective tokens saved per midnight crossing.
- Announcement UX: initial vs. delta headers; actionable removal wording; "use ToolSearch for
  details" on-demand docs.
- Cost of getting it wrong: agent list embedded in a tool description caused ~10.2% of fleet
  cache_creation; a stateless scan bug (inc-4747) required per-call-site diagnostics.

What SCM does differently:

- **Self-describing chain** (`EpochId`/`Seq`/`PrevSeq`) instead of scan-only reconstruction —
  stronger against gaps, deletions and partial cuts.
- **Unified section-provider architecture** instead of per-surface hardcoded patches.
- **Checkpoints are first-class** (shields, summary edits, compaction) and always trigger
  rebaseline; no sliding window in hybrid mode.
- **User-configurable modes** (dynamic/static/hybrid) and a first-class inspector.

## 16. Open questions

1. **Hidden addons:** announce presence or stay silent (while remaining callable)?
   (Recommendation: silent.)
2. **Refresh-note on rebaseline:** always emit ("baseline refreshed: …"), only when changes exist,
   or never? (Recommendation: emit when there are visible changes.)
3. **Thresholds** for text sections: when to emit full replacement vs. summarized diff.
4. **Carry vs. re-announce token budgets** per section (measure and set defaults).
5. **Static snapshot scope:** per-agent config, shared across all chats of that agent — confirm.
6. **Provider smoke tests:** validate "later-announced tool call" passthrough on each supported
   provider (expected to pass; strict OpenAI-compatible gateways are the risk).
7. **Schema evolution:** versioning + legacy object handling for old chats (a `LEGACY_*` registry
   analogous to Claude Code's).
8. **Timezone section in v0** — include now or v1?
9. **Sub-agents / agentic tasks** — eventual SCM scope, explicitly deferred from v0.

---

*Revision history: v0 draft — assembled from the SCM design discussion (September 2026).*
