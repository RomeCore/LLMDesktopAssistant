# Onboarding & Documentation

> **Status:** Problem  
> **Priority:** High  
> **Tags:** docs, ux, onboarding

## Problem

dASS has zero onboarding. A first-time user sees an empty chat and has no idea:

- How to connect a model
- Which models/providers are supported
- How to configure agents
- What MCP is and how to set it up
- How to write Lua scripts
- How to create custom tools

No tutorials, no tooltips, no built-in documentation.

## What Needs to Be Done

### 1. Built-in Onboarding (Welcome Flow)

On first launch, show a **step-by-step wizard**:

- **Step 1:** Choose LLM provider (Ollama, OpenAI-compatible, Deepseek, OpenRouter)
- **Step 2:** Connect a model (test query)
- **Step 3:** Quick UI overview (chat, agents, tools)
- **Step 4:** Offer to create a first agent or start chatting

### 2. Examples & Templates

- **Prompt examples** — "Load example" button in `PromptManagerView`
- **Agent templates** — presets for common scenarios (coder, researcher, DM for D&D)
- **Lua script examples** — built-in collection with explanations

### 3. In-App Documentation

- **"Help"** section in menu or sidebar
- **Feature highlights** — subtle badges/tooltips when a feature appears first time
- **Field descriptions** — already partially done via `[Description]` attributes

### 4. Interactive Tutorials

- "Try `calculate 2+2`"
- "Create your first agent"
- "Write a Lua script that reads a file"
- "Connect an MCP server"

## UI Notes

- Onboarding progress bar
- Ability to skip / come back later
- "What's this?" button next to unfamiliar elements

## Priority

**High** — without this, new users can't comfortably start using dASS.
