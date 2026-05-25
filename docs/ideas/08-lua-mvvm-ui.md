# Lua MVVM: Dynamic UI from Scripts

> **Status:** Proposal  
> **Priority:** Low/Experimental  
> **Goal:** Allow Lua scripts to create Avalonia UI dynamically using MVVM, enabling rich interactive tools with custom interfaces.

## Motivation

Current tools can only interact with the user via:
- `forms-input`, `forms-choice`, `forms-confirm` — built-in form tools
- `ReactiveToolResult` — status text, icon, progress

But a tool cannot create **custom UI** — complex forms, data grids, interactive visualizations, etc.

With Lua MVVM, any meta tool can render **arbitrary Avalonia UI** directly in the chat:

```lua
-- A weather tool that shows a custom card
local axaml = [[
<Border xmlns="https://github.com/avaloniaui"
        Background="{Binding bg_color}"
        CornerRadius="8" Padding="16"
        Width="300">
  <StackPanel Spacing="8">
    <TextBlock Text="{Binding city}" FontSize="24" FontWeight="Bold"/>
    <TextBlock Text="{Binding temperature}" FontSize="48"/>
    <TextBlock Text="{Binding description}"/>
    <Button Content="Refresh" Command="{Binding refresh}" />
  </StackPanel>
</Border>
]]

local vm = dass.mvvm.create({
  city = "London",
  temperature = "22°C",
  description = "Partly cloudy",
  bg_color = "#1a1a2e",
  refresh = function()
    local w = web.fetch("https://api.weather.com/london")
    vm.temperature = w.temp .. "°C"  -- auto-notify UI
  end
})

dass.mvvm.show(axaml, vm)
```

## Architecture

```
┌─────────────────────────────────────────────┐
│                 dASS UI (Avalonia)            │
│                                               │
│  ┌─────────────────────────────────────────┐  │
│  │  Chat Message                           │  │
│  │                                         │  │
│  │  ┌──────────────────────────────────┐   │  │
│  │  │  Tool Call: "show-weather"       │   │  │
│  │  │                                  │   │  │
│  │  │  ┌──────────────────────────┐    │   │  │
│  │  │  │  Lua-generated UI        │    │   │  │
│  │  │  │  (ContentControl bound    │    │   │  │
│  │  │  │   to Lua ViewModel)      │    │   │  │
│  │  │  └──────────────────────────┘    │   │  │
│  │  └──────────────────────────────────┘   │  │
│  └─────────────────────────────────────────┘  │
└─────────────────────────────────────────────┘
```

## API Design

### Core Functions

```lua
--[[
dass.mvvm — Dynamic UI creation from Lua scripts
]]

--- dass.mvvm.create(initial_data)
-- Creates a ViewModel (observable table) from a Lua table.
-- String/number/boolean fields become observable properties.
-- Function values become commands (can be bound to Button.Command etc.)
-- Returns a proxy table where setting a property auto-notifies the UI.
-- @param initial_data: table — initial property values
-- @return table — observable ViewModel

--- dass.mvvm.load(axaml_string)
-- Parses an AXAML string and returns a parsed template.
-- The template can be bound to a ViewModel and rendered.
-- @param axaml: string — valid Avalonia XAML
-- @return template: userdata — compiled template

--- dass.mvvm.bind(template, view_model)
-- Creates a UI element from a template bound to a ViewModel.
-- Returns an Avalonia Control ready to be inserted into the chat.
-- @param template: userdata — compiled template (from load)
-- @param view_model: table — observable ViewModel (from create)
-- @return control: userdata — Avalonia Control

--- dass.mvvm.show(axaml, view_model)
-- Convenience: loads AXAML, binds ViewModel, and shows in chat.
-- Equivalent to: dass.mvvm.bind(dass.mvvm.load(axaml), vm)
-- @param axaml: string — AXAML markup
-- @param view_model: table — ViewModel

--- dass.mvvm.context
-- Returns the current tool execution context (ToolExecutionContext).
-- Allows access to chat, message, and other context info.
-- @return table with fields: chat, message, tool_call, info
```

### Complete Example: Confirmation Dialog with Extra UI

```lua
-- A tool that shows a file preview with confirm/cancel
local axaml = [[
<Border xmlns="https://github.com/avaloniaui"
        Background="#2d2d2d" CornerRadius="8" Padding="16">
  <StackPanel Spacing="12" MinWidth="400">
    <TextBlock Text="{Binding title}" FontSize="18" FontWeight="Bold"/>
    <ScrollViewer MaxHeight="300">
      <TextBox Text="{Binding content}" IsReadOnly="True"
               FontFamily="JetBrains Mono" FontSize="12"/>
    </ScrollViewer>
    <StackPanel Orientation="Horizontal" Spacing="8" HorizontalAlignment="Right">
      <Button Content="Edit" Command="{Binding edit}" />
      <Button Content="Delete" Command="{binding delete}"
              Background="#c0392b" Foreground="White"/>
      <Button Content="Cancel" Command="{Binding cancel}" />
    </StackPanel>
  </StackPanel>
</Border>
]]

local file_content = fs.read("path/to/file.txt")
local vm = dass.mvvm.create({
  title = "Preview: file.txt",
  content = file_content,
  edit = function()
    local r = dass.tools.call("fs-write_file", {
      path = "path/to/file.txt",
      content = dass.mvvm.prompt("Edit file:", file_content)
    })
    vm.content = r.content  -- update preview
  end,
  delete = function()
    local confirmed = dass.tools.call("forms-confirm", {
      title = "Confirm delete",
      description = "Are you sure?"
    })
    if confirmed.success then
      fs.delete("path/to/file.txt")
      dass.mvvm.close()  -- close this UI
    end
  end,
  cancel = function()
    dass.mvvm.close()
  end
})

dass.mvvm.show(axaml, vm)
```

## Implementation Plan

### 1. Observable Lua Table (ViewModel)

Create a Lua metatable that fires change notifications when properties are set:

```csharp
public class LuaObservableViewModel : INotifyPropertyChanged
{
    private readonly Script _script;
    private readonly Table _source;
    private readonly Dictionary<string, LuaCommand> _commands = new();
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public LuaObservableViewModel(Script script, Table source)
    {
        _script = script;
        _source = source;
        
        // Wrap function values as commands
        foreach (var (key, value) in source)
        {
            if (value.Type == DataType.Function)
            {
                var cmd = new LuaCommand(value.Function, script);
                _commands[key.String] = cmd;
                source[key] = DynValue.NewUserData(cmd);
            }
        }
        
        // Set up metatable for change notification
        source.MetaTable = CreateMetaTable(script);
    }
}
```

### 2. Lua Command (ICommand)

Wrap Lua functions as WPF/Avalonia commands:

```csharp
public class LuaCommand : ICommand
{
    private readonly Closure _function;
    private readonly Script _script;
    
    public event EventHandler? CanExecuteChanged;
    
    public bool CanExecute(object? parameter) => true;
    
    public void Execute(object? parameter)
    {
        _script.Call(_function, parameter);
    }
}
```

### 3. AXAML Parser + Data Binding

Use Avalonia's built-in XAML loading with data binding:

```csharp
public class LuaMvvmService
{
    private readonly LuaService _luaService;
    
    public Control LoadAndBind(string axaml, LuaObservableViewModel vm)
    {
        // 1. Parse AXAML
        var parser = new AvaloniaXamlLoader();
        var control = (Control)parser.Load(axaml);
        
        // 2. Set DataContext
        control.DataContext = vm;
        
        // 3. Return control for insertion into chat
        return control;
    }
}
```

### 4. Integration with Chat Messages

The `AssistantMessage` can hold additional UI controls:

```csharp
public class AssistantMessage
{
    // Existing properties...
    
    /// <summary>
    /// Additional Avalonia controls created by Lua scripts.
    /// They are rendered below the message content.
    /// </summary>
    public ObservableCollection<Control> AdditionalControls { get; } = new();
}
```

Then in the chat view, render these controls:

```xml
<!-- In AssistantMessageView.axaml -->
<ItemsControl ItemsSource="{Binding AdditionalControls}" />
```

### 5. Lua API Registration

```csharp
[LuaApi]
public class LuaApiMvvm : LuaApiBase
{
    public override string? Namespace => "dass.mvvm";
    
    // create(initial_data) → view_model
    // load(axaml) → template
    // bind(template, vm) → control
    // show(axaml, vm) → void
    // close() → void
    // prompt(text, default) → string
    // context → table
}
```

## Use Cases

| Scenario | Example |
|----------|---------|
| **Custom forms** | Multi-field input with validation |
| **Data visualization** | Tables, charts, progress indicators |
| **File preview** | Syntax-highlighted code viewer with edit/delete buttons |
| **Image gallery** | Thumbnail grid from web search results |
| **Interactive wizards** | Multi-step guided workflows |
| **Real-time dashboards** | Live-updating status panels |
| **Settings editors** | Dynamic configuration UI |
| **Chat games** | Tic-tac-toe, trivia with buttons |
| **Debug tools** | Object inspector, JSON tree viewer |

## Benefits

- ✅ **Infinite UI possibilities** — not limited to built-in forms
- ✅ **MVVM pattern** — clean separation of logic and presentation
- ✅ **Full Avalonia power** — any control, styling, animations
- ✅ **Reactive** — property changes auto-update UI
- ✅ **Commands** — buttons and inputs work natively
- ✅ **No compilation** — UI is defined at runtime in Lua
- ✅ **Chat integration** — UI appears inline in the conversation

## Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| **XAML injection** | Sandboxed Lua, no file system access to arbitrary paths |
| **Performance** | Complex UIs can be cached; limit per-message controls |
| **Memory leaks** | ViewModels are GC'd when message is removed |
| **Thread safety** | All UI updates must be dispatched to UI thread |
| **Security** | Lua cannot access Win32 APIs directly; XAML bindings are read-only by default |

## Future Extensions

- **Blazor Web UI** — same Lua MVVM rendered as HTML/Blazor components for web clients
- **ReactiveUI** — integration with ReactiveUI for more advanced patterns
- **Custom styles** — Lua can define Avalonia styles dynamically
- **Animations** — trigger animations from Lua
- **Charts** — built-in chart controls bound to Lua data
