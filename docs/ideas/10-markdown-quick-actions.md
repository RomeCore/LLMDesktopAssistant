# Markdown Quick Actions (Clickable Suggestions)

> **Status:** Idea
> **Priority:** High
> **Tags:** ui, markdown, ux, productivity

## Problem

After the LLM generates a response (code, explanation, analysis), the user frequently wants to follow up with common actions like:

- "Write tests for this code"
- "Explain this in more detail"
- "Refactor this"
- "Find bugs"
- "Translate to Russian"
- "Optimize this"

Currently, the user must **manually type** these follow-up requests. This is friction — especially on mobile or when iterating rapidly.

## Proposed Solution

Introduce **Quick Actions** — a Markdown extension that renders as **clickable chips/buttons** inline in the assistant's response. When clicked, the button text is automatically inserted into the user input field and submitted.

### Syntax

```
The code is complete! Here's what I'd suggest next:

^[Write unit tests](Write comprehensive unit tests for this code)
^[Explain in detail](Explain this code step by step)
^[Find bugs](Analyze this code for potential bugs and security issues)
^[Optimize](Refactor this code for better performance)
^[Add comments](Add detailed XML documentation comments to this code)
```

Renders as:

```
The code is complete! Here's what I'd suggest next:

[Write unit tests] [Explain in detail] [Find bugs]
[Optimize] [Add comments]
```

### Variants

#### 1. Simple Quick Action
```
^[Button Text](Prompt text)
```
- `Button Text` — visible label on the button
- `Prompt text` — text that gets inserted into user input

#### 2. Quick Action with Context
```
^[Refactor](Refactor this code) with focus on {readability}
```
- Uses selected text or context from the response

#### 3. Multi-step Quick Action
```
^[Create PR](Create a pull request for these changes)
---
^[The PR description will include:
- Summary of changes
- Related issues
- Testing notes]
^[Confirm](Yes, create the PR) ^[Cancel](Never mind, let me review again)
```

#### 4. Quick Action Group
```
^^[Actions]
^[Test](Write tests)
^[Doc](Add documentation)
^[Review](Request code review)
^^[/Actions]
```
Groups buttons visually together.

### Implementation

```csharp
// Markdown renderer detects ^[...](...) pattern
public class QuickActionInline : IInline
{
    public string ButtonText { get; set; }
    public string Prompt { get; set; }
}

// In MarkdownControl.axaml:
// - Render QuickActionInline as a styled Button
// - On click: set ChatViewModel.UserInput = Prompt; submit

public class QuickActionRenderer
{
    private static readonly Regex _quickActionPattern = 
        new Regex(@"\^\[([^\]]+)\]\(([^)]+)\)");
    
    public static MarkdownDocument Process(string markdown)
    {
        var document = Markdown.Parse(markdown);
        // Find any paragraph inlines and convert QuickAction patterns
        // to custom QuickActionInline objects
        return document;
    }
}
```

### UI Behavior

1. **Hover** — Button gets highlighted, cursor changes to pointer
2. **Click** — Prompt text is inserted into user input field
3. **Shift+Click** — Prompt text is inserted but NOT submitted (user can edit)
4. **Right-click** — Context menu: "Copy prompt text", "Copy as quote"
5. **Keyboard** — Tab to navigate between buttons, Enter to activate

### Styling (Avalonia)

```xml
<Style Selector="Button.quick-action">
    <Setter Property="Background" Value="{StaticResource AccentColor}" />
    <Setter Property="CornerRadius" Value="16" />
    <Setter Property="Padding" Value="12,4" />
    <Setter Property="Margin" Value="4,2" />
    <Setter Property="FontSize" Value="13" />
    <Setter Property="Cursor" Value="Hand" />
    <Setter Property="Foreground" Value="{StaticResource AccentColorForeground}" />
</Style>

<Style Selector="Button.quick-action:pointerover">
    <Setter Property="Opacity" Value="0.85" />
    <Setter Property="Transform">
        <ScaleTransform ScaleX="1.02" ScaleY="1.02" />
    </Setter>
</Style>
```

### Integration with Existing Markdown

| Existing Feature | Interaction |
|---|---|
| `MarkdownControl.axaml` | New renderer for QuickActionInline |
| `AssistantMessageTextPartView` | Contains the markdown flow document |
| `UserInputView` | Target for prompt text injection |
| `ChatViewModel.UserInput` | Property to set input text |
| `PromptChatBuilder` | Could suggest actions based on context |

### Example Workflows

#### Code Generation
```
Here's a REST API client:

```python
import requests

class APIClient:
    def __init__(self, base_url):
        self.base_url = base_url
    def get(self, path):
        return requests.get(f"{self.base_url}{path}")
```

Ready to use! Want me to:

^[Add error handling](Add proper error handling and retries to this API client)
^[Write tests](Write unit tests for this API client using pytest and responses mock)
^[Use async](Rewrite this API client to use async/await with aiohttp)
^[Add docs](Add comprehensive docstrings and type hints)
```

#### Bug Fix Explanation
```
The issue was a null reference in the `LoadUser` method when 
`User.Database` is not initialized before first access.

^[Apply fix](Apply this fix to the codebase)
^[Explain more](Explain why this null reference occurs in detail)
^[Related patterns](Show me other places in the codebase with similar patterns)
^[Add guard](Add a null check guard clause to all similar methods)
```

#### Analysis Result
```
Code analysis found 3 potential issues:
1. Memory leak in event handler (line 42)
2. SQL injection risk (line 78)
3. Unused variable (line 15)

^[Fix issue 1](Fix the memory leak in event handler at line 42)
^[Fix all](Fix all 3 issues found in the analysis)
^[Generate report](Generate a detailed PDF report of these findings)
^[Dismiss](I'll handle these manually, thanks)
```

### Benefits

- ✅ **Zero typing** — one click to continue the conversation
- ✅ **Context-aware** — buttons carry full prompt context
- ✅ **Non-intrusive** — renders as standard buttons in the flow
- ✅ **Backward compatible** — `^[...](...)` is not standard Markdown, so existing renderers ignore it
- ✅ **LLM-controlled** — the AI decides what actions to suggest
- ✅ **Guidable** — user can say "Suggest actions for testing" to influence suggestions

### Implementation Phases

| Phase | What |
|---|---|
| **1. Parser** | Regex parser for `^[...](...)` pattern in markdown |
| **2. Renderer** | Custom Avalonia inline control rendering as buttons |
| **3. Click handler** | Wire click → set UserInput → submit |
| **4. Styling** | Theme-aware button styles |
| **5. System prompt** | Instruct LLM when/how to suggest actions |
| **6. Context injection** | Allow LLM to reference specific code blocks |
| **7. Multi-action groups** | Group related actions visually |
| **8. Keyboard navigation** | Tab, Enter, Shift+Enter support |

### Open Questions

- Should user be able to customize suggested actions via system prompt?
- Should actions be savable as "quick commands" for reuse?
- How to handle very long prompt text (show tooltip)?
- Should there be a "thumbs up/down" on suggested actions for LLM feedback?
