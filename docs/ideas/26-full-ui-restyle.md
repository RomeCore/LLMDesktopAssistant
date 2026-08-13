# Full UI Restyle: Consistent Theme, Tabs & Control Audit

> **Status:** Problem + Feature Idea  
> **Priority:** Medium  
> **Tags:** ui, ux, restyle, theme, tabs

## Problem

The current theme has grown organically and shows it:

1. **Inconsistent paddings** — base `ListBoxItem` style uses `Padding="12 9 12 12"`, which is
   excessive for compact tab bars. Chat tab status background (added in `feat(chat)` commit
   `429a8a6`) was rendered *smaller* than the tab itself until a scoped workaround
   (`ListBox.ChatTabsListBox > ListBoxItem { Padding = 0 }`) was applied. Sidebar items and
   tabs still have different visual density.
2. **Hardcoded colors** — many controls use raw hex values (`#2A2A3E`, `#3A3A7E`, `#4A4A8E`,
   `#EAEAEA`, etc.) instead of theme resources, making restyle and dark/light switching painful.
3. **Inconsistent corner radii** — 4 vs 6 px across controls (`ListBoxItem` = 4, chat tab = 6).
4. **Mixed theming approaches** — some styling lives in `Styles/Theme/Controls/*.axaml`,
   some inline in views, some as magic hex strings in view models (e.g. `StatusColorHex`
   in `ChatManagerViewModel` returns `"#3342A5F5"` / `"#33FFC107"`).
5. **No design tokens** — spacing, radius, colors and durations are not centralized.

## Proposed Changes

### 1. Design tokens

Introduce a central resource dictionary with named tokens:

| Token group | Examples |
|---|---|
| Colors | `TabStatusExecuting`, `TabStatusConfirming`, `HoverBackground`, `SelectedBackground` |
| Spacing | `SpacingXSmall`, `SpacingSmall`, `SpacingMedium`, `ItemPadding*` |
| Radii | `RadiusSmall` (4), `RadiusMedium` (6), `RadiusLarge` (8) |
| Durations | `TransitionFast` (150ms), `TransitionNormal` (400ms) |

Replace all hardcoded hex colors in `Styles/Theme/Controls/*.axaml` with `DynamicResource` references.

### 2. Unified ListBoxItem / tab system

- Single `ListBoxItem` base style with sensible compact padding
- A `Tab` variant (class-based) for chat tabs — full-bleed content, status-aware background
- Move status colors from `StatusColorHex` (VM string) into theme resources; keep the VM
  returning a *key* or use a converter

### 3. Consistent controls

- Audit `Expander`, `Slider`, `ScrollBar`, buttons for padding/radius/color consistency
  (partially covered by idea #22)
- One corner radius scale, one hover/pressed/selected color scale

### 4. Status UI cleanup (follow-up to `429a8a6`)

- Keep `IChatExecutionStatusService` as the single source of truth
- Move `StatusIcon`/`StatusColorHex` mapping into a converter or attached behavior
  instead of VM string colors
- Revisit taskbar progress integration (`ITaskbarList3`) — previously deferred

## Implementation Notes

- Everything in `Styles/Theme/Controls/*.axaml`, one file per component
- Use `DynamicResource` everywhere; no raw colors in views or VMs
- Respect OS "Reduce motion" setting for transitions
- Test dark/light/system theme switching after the change

## Priority

**Medium** — planned as a full restyle pass in the near future; the chat tab workaround
(`ChatTabsListBox` padding override) can be reverted once tokens are in place.
