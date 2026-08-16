# Agents

dASS supports multiple specialized agents that collaborate on tasks.

## How agents work

Each agent has its own:

- **Persona** and **specialization** — how it behaves and what it knows
- **Toolset** — which tools it is allowed to use
- **Read permissions** — what it can see in the conversation

## Execution strategies

- **Sequential** — agents answer one after another
- **Random** — a random agent is picked each round
- **Adaptive** — the router decides which agent fits best
- **Mention-only** — agents answer only when mentioned
- **Round-robin** — agents take turns

> [!TIP]
> Configure agents in *Settings → Agents* or per chat in the chat settings.
