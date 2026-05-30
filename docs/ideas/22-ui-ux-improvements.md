# UI/UX Improvements: Styles, Animations & Sidebar Redesign

> **Status:** Problem + Feature Idea  
> **Priority:** Medium  
> **Tags:** ui, ux, styling, animations, sidebar

## Problem

The current UI has several rough edges:

1. **Broken styles** — `Expander` and `Slider` controls look wonky (wrong padding, alignment, colors)
2. **No animations** — transitions are instant, no smooth feels (message appearing, sidebar toggling, tool status changes)
3. **Cluttered input area** — `UserInputView` has too many buttons (send, attach, tools, settings), needs reorganization
4. **Missing sidebar** — tools like MCP Manager, Prompt Manager, Settings are scattered across windows/dialogs instead of being accessible from a sidebar

## Proposed Changes

### 1. Fix Broken Styles

- **Expander** — proper header styling, smooth expand/collapse animation, consistent padding
- **Slider** — fix track/thumb alignment, add value tooltip, proper colors matching theme
- **ScrollBar** — consistent width, auto-hide behavior
- **Button hover states** — add micro-interactions (scale, color shift)
- **Consistent spacing** — audit all controls for margin/padding consistency

### 2. Animations

| Element | Animation | Duration |
|---------|-----------|----------|
| **Message appearing** | Fade in + slide up | 200ms |
| **Sidebar toggle** | Slide left/right with easing | 250ms |
| **Tool status change** | Icon morph + color transition | 150ms |
| **Reaction placement** | Emoji pop animation | 300ms |
| **Button hover** | Scale 1.02 + slight glow | 100ms |
| **Tab switch** | Cross-fade | 200ms |
| **Dialog open/close** | Scale + fade | 200ms |
| **Tool progress** | Smooth progress bar fill | linear |

All animations should:
- Respect OS "Reduce motion" setting
- Be GPU-accelerated (Avalonia renders)
- Not block UI thread

### 3. UserInput Redesign

Current state: many buttons cramped in a single row.

**New layout:**
```
┌─────────────────────────────────────────────────────┐
│ [Attach]  [Input Text Field........................] │
│ [Send ▶]  [Mic 🎤]                               │
│                                                     │
│ [Tools ▲]  [Agents ▼]  [MCP ■]  [Prompts ✏️]      │
│                    (contextual, not always visible)  │
└─────────────────────────────────────────────────────┘
```

- Primary actions (send, attach, mic) — always visible
- Secondary actions (MCP, Prompts, Settings) — collapsible or moved to sidebar
- Toolbar auto-hides when not needed

### 4. Sidebar System

Instead of separate windows for each manager, introduce a **unified sidebar**:

**Left sidebar** (chat list):
- Chat list with categories
- New chat button
- Search/filter
- Collapsible

**Right sidebar** (contextual tools):
| Panel | Content |
|-------|---------|
| **MCP Manager** | Connected servers, tool list, add/remove |
| **Prompt Manager** | Template browser, editor, variables |
| **Agent Settings** | Current agent configuration |
| **Tool Browser** | All available tools, search, filter |
| **Script Console** | Lua/Python REPL, script output |
| **Settings** | App settings, model config, provider setup |

- Tabs at the top of the sidebar
- Each panel is a separate `UserControl`
- Sidebar width is draggable
- Remembers last open panel
- Can be hidden entirely for minimal mode

### 5. Theme Improvements

- **Dark/Light/System** theme switching (already partially works)
- **Accent color picker** — user-selectable accent color
- **Custom background** — optional chat background image
- **Font size** — per-element font size settings (code, chat, UI)

### 6. Implementation Notes

- Use Avalonia's built-in animation framework (`Animation`, `Transition`)
- Styles should be in separate `.axaml` files per component
- Sidebar uses `DockPanel` or `GridSplitter` pattern
- Animation performance: prefer `RenderTransform` over `Margin` changes

## Priority

**Medium** — visual polish matters for user retention but doesn't affect functionality.
