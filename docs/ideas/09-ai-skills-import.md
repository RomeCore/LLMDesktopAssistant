# AI Skills Import from Repositories

> **Status:** Idea
> **Priority:** Medium
> **Tags:** prompting, skills, community, sharing

## Problem

Current prompt engineering system (LLT templates, personas, specializations, sliders) is powerful but **isolated** — users must manually create and manage all prompts within the app. There's no way to **discover, share, and import** ready-made AI skills from the community or from project repositories.

Meanwhile, many open-source projects already include `SKILL.md` or similar files that describe how AI agents should interact with the codebase (e.g., coding conventions, architecture, testing patterns).

## Proposed Solution

Create a system for importing AI skills from external repositories that contain a `SKILL.md` (or similar) manifest file.

### SKILL.md Format

```markdown
# Skill: Rust Development

## Description
Expert Rust developer with deep knowledge of async programming, unsafe code, and the Rust ecosystem.

## Persona
You are an expert Rust developer. Follow Rust idioms, use `Result`/`Option` properly,
prefer iterator combinators, and always consider memory safety.

## Specializations
- Systems Programming
- WebAssembly
- CLI Tools

## Components (optional)
- `@context/cargo-toml` — Reads Cargo.toml for project context
- `@rules/testing` — Always add unit tests

## Sliders
- formality: 0.7       # Formal tone
- creativity: 0.3      # Conservative
- verbosity: 0.5       # Balanced

## Tools (optional)
- Recommended tools: fs-read, fs-write, bash, web-search

## Hooks (optional)
- `on_project_open` — Runs when opening a Rust project
- `on_build_error` — Analyzes build errors

## Dependencies (optional)
- Requires: "rust-analyzer" LSP
- Optional: "cargo-nextest" for testing

## Version
1.0.0
```

### Discovery Sources

1. **Repository scanning** — When user opens a project, scan for `SKILL.md` in the root
2. **Skill Registry** — A curated registry (GitHub repo index) of community skills
3. **URL import** — Import directly from `https://github.com/user/repo/blob/main/SKILL.md`
4. **CLI search** — `claw skill search "rust webassembly"` → finds matching skills
5. **Auto-detect** — Use LLM to analyze project structure and suggest matching skills

### Architecture

```
┌─────────────────────────────────────────────────────┐
│                  Skill Manager                       │
│                                                      │
│  ┌──────────────┐  ┌──────────────┐  ┌────────────┐ │
│  │  Repository   │  │   Registry   │  │   Local    │ │
│  │  Scanner     │  │   Fetcher    │  │   Cache    │ │
│  └──────┬───────┘  └──────┬───────┘  └──────┬─────┘ │
│         │                │                  │        │
│         ▼                ▼                  ▼        │
│  ┌────────────────────────────────────────────────┐  │
│  │              Skill Parser                        │  │
│  │  (Parses SKILL.md → internal skill objects)    │  │
│  └────────────────────┬───────────────────────────┘  │
│                       │                              │
│                       ▼                              │
│  ┌────────────────────────────────────────────────┐  │
│  │            Prompt Integration                    │  │
│  │  - Persona → AgentDescriptor.Persona            │  │
│  │  - Specializations → AgentDescriptor.Specs      │  │
│  │  - Components → PromptRegistry Components        │  │
│  │  - Sliders → BehaviorSlider defaults             │  │
│  │  - Hooks → PromptBuildingHooks                   │  │
│  └────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────┘
```

### Integration with Existing System

| Skill Component | Existing dASS Mapping |
|---|---|
| `persona` | `AgentPromptSettings.Persona` |
| `specializations` | `SpecializationsConfiguration` |
| `components` | `PromptRegistry.GetComponent()` |
| `sliders` | `BehaviorSliderValue` |
| `tools` | `AgentToolSettings` |
| `hooks` | `IPromptBuildingHook` / `IPromptInjector` |

### UI

```
┌──────────────────────────────────────────────────────┐
│  Skills Manager                                [+ Add] │
├──────────────────────────────────────────────────────┤
│  ┌──────────────────────────────────────────────┐    │
│  │ 🔍 Search skills...                         │    │
│  └──────────────────────────────────────────────┘    │
│                                                      │
│  Installed Skills:                                   │
│  ┌──────────────────────────────────────────────┐    │
│  │ 🦀 Rust Development                     v1.0 │    │
│  │    Expert Rust developer skillset              │    │
│  │    [Disable] [Edit] [Uninstall]               │    │
│  ├──────────────────────────────────────────────┤    │
│  │ 🐍 Python Data Science                  v2.1 │    │
│  │    NumPy, Pandas, Matplotlib expertise        │    │
│  │    [Disable] [Edit] [Uninstall]               │    │
│  ├──────────────────────────────────────────────┤    │
│  │ 🌐 Web Developer                        v1.3 │    │
│  │    React, TypeScript, Tailwind CSS expert     │    │
│  │    [Disable] [Edit] [Uninstall]               │    │
│  └──────────────────────────────────────────────┘    │
│                                                      │
│  Available from Registry:                            │
│  ┌──────────────────────────────────────────────┐    │
│  │ 📱 Mobile (Flutter)                    ⭐ 234 │    │
│  │ 📦 Blockchain/Solidity                 ⭐ 189 │    │
│  │ 🎮 Game Dev (Unity)                    ⭐ 156 │    │
│  └──────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────┘
```

### Benefits

- ✅ **Community-driven** — share and discover AI skills
- ✅ **Project-aware** — auto-import relevant skills per project
- ✅ **Composable** — multiple skills can be active simultaneously
- ✅ **Versioned** — skills can evolve with projects
- ✅ **Portable** — SKILL.md can live in any repository
- ✅ **Extends existing system** — doesn't replace it

### Open Questions

- How to handle skill conflicts when multiple skills modify same prompt parts?
- Should skills support conditional activation (e.g., only when certain files detected)?
- How to verify/trust community skills (sandbox evaluation)?
- How to handle skill updates from remote repositories?
