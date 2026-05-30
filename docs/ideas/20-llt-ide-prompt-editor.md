# LLT IDE — Built-in Prompt Editor

> **Status:** Feature Idea  
> **Priority:** Medium  
> **Tags:** prompting, ux, editor, llt

## Problem

LLT templates (`.llt` files) are currently edited **outside** the application — in Notepad, VS Code, etc. There's no:

- Syntax highlighting for LLT
- Variable/component suggestion
- AI-assisted prompt improvement
- Validation (missing variables, syntax errors)
- UI for external/user-defined variables

## Proposed Solution

### 1. Built-in LLT Editor

A full-featured code editor for LLT templates inside dASS:

- **Syntax highlighting** — `@template`, `@foreach`, `@if`, `@metadata`, variables, etc.
- **Line numbers** and code folding
- **Error markers** — red squiggles for syntax errors
- **Quick actions** — "Insert component", "Add slider", "Add metadata"

### 2. IntelliSense / Auto-Completion

- **Variable suggestions** — `@` triggers dropdown of available variables
- **Component names** — list registered components from `PromptRegistry`
- **Slider IDs** — list available behavior sliders
- **Persona/Specialization names** — autocomplete from configuration

### 3. External Variables UI

LLT templates can define **external variables** — values not provided by the system but expected from the user:

```handlebars
@template my_custom_prompt {
  @external name = "World"        {{! User-editable, default "World" }}
  @external temperature = 0.7     {{! Slider in UI }}
  @external style = "formal"      {{! Dropdown: formal, casual, technical }}
  
  Hello, @name!
  Please respond with @style tone.
}
```

These would render as **UI controls** in the Prompt Manager:
- Text field for `name`
- Slider for `temperature`
- Dropdown for `style`

### 4. AI-Assisted Prompt Improvement

Select a prompt template and ask the LLM to improve it:

- **"Make this more concise"**
- **"Add markdown formatting guidelines"**
- **"Make it sound more professional"**
- **"Translate to Russian"**
- **"Add few-shot examples"**

Powered by a dedicated agent that reads the template and suggests changes with diff preview.

### 5. Diff View

When editing templates, show a **diff view** (before/after) to review changes before saving.

### 6. Implementation Notes

- Editor component: could use AvaloniaEdit or a simpler custom TextEditor
- Syntax highlighting: custom `IClassificationHighlighters` for Avalonia
- External variables: new `@external` directive in LLTSharp parser
- AI improvement: reuse existing `IPromptChatBuilder` infrastructure

## Data Model

```csharp
public class LltTemplateVariable
{
    public string Name { get; init; }
    public string Type { get; init; }  // "string", "number", "enum", "boolean"
    public string? DefaultValue { get; init; }
    public string[]? EnumValues { get; init; }
    public string? Description { get; init; }
}

public class LltTemplate
{
    public string RawContent { get; set; }
    public List<LltTemplateVariable> Variables { get; set; }
    public Dictionary<string, object> VariableValues { get; set; }  // User-set values
}
```

## Priority

**Medium** — accelerates prompt engineering workflow significantly.
