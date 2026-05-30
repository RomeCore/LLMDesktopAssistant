# Lua-Scriptable Services

> **Status:** Feature Idea  
> **Priority:** Low/Experimental  
> **Tags:** lua, scripting, extensibility, plugins

## Problem

Currently, Lua scripts can only be used as **meta tools** (execute-lua) or via `dass.tools.call()`. But many internal systems are hardcoded in C# and cannot be modified by users:

- **ExecutionStages** — can't create a custom agent selection strategy without writing C#
- **MessageExtensions** — can't add custom UI elements to messages
- **CodeBlockExtensions** — can't add custom code block renderers
- **ToolModules** — can't create a new tool module from Lua (only individual tools via `metatools-create_or_update`)
- **PromptInjectors / Hooks** — can't inject custom prompt logic

## Proposed Solution

Allow Lua scripts to **implement interfaces** that dASS discovers and uses:

### 1. Lua Execution Stage

```lua
--[[
title: MyCustomStage
description: Custom agent selection logic
category: agents
implements: ExecutionStage
]]
-- This function is called when the stage needs to select the next agent
function get_next_agent(context)
    local agents = context.agent_instances
    -- Pick the agent with the most recent message
    local best = nil
    local best_time = nil
    for _, agent in ipairs(agents) do
        if agent.enabled then
            local last_msg = dass.tools.call("agent-get_last_message", {agent_id = agent.id})
            if last_msg.success then
                if best == nil or last_msg.structured.timestamp > best_time then
                    best = agent.id
                    best_time = last_msg.structured.timestamp
                end
            end
        end
    end
    return best
end
```

### 2. Lua Message Extension

```lua
--[[
title: CodeReviewExtension
description: Adds a "Review" button to code blocks
implements: MessageExtension
target: CodeBlock
]]
function on_message_rendered(message, context)
    if message:contains_code() then
        local axaml = [[
            <Button Content="Review Code" Classes="QuickAction" />
        ]]
        local vm = dass.mvvm.create({
            on_click = function()
                local review = dass.tools.call("agent-ask_question", {
                    text = "Review this code: " .. message.code_content
                })
                dass.ui.show_notification(review.content)
            end
        })
        return dass.mvvm.bind(axaml, vm)
    end
    return nil
end
```

### 3. Lua Code Block Extension

```lua
--[[
title: MermaidRenderer
description: Renders mermaid diagrams from code blocks
implements: CodeBlockExtension
target: mermaid
]]
function render_code_block(language, code, context)
    if language == "mermaid" then
        -- Use a JavaScript library to render
        local html = "<div class='mermaid'>" .. code .. "</div>"
        local result = dass.tools.call("webui-inject_html", {html = html})
        return result.content
    end
    return nil  -- Fall back to default renderer
end
```

### 4. Lua Tool Module

Since `metatools-create_or_update` already allows creating individual tools, this is partially solved. But a Lua Tool Module could:

- Register multiple tools at once
- Share state between tools
- Use lifecycle hooks (on_register, on_unregister)

```lua
--[[
title: MyToolModule
description: A collection of weather-related tools
implements: ToolModule
]]
function tools()
    return {
        {
            name = "weather-current",
            description = "Get current weather",
            execute = function(args)
                local r = web.fetch("https://api.weather.com/current?city=" .. args.city)
                return r.content
            end
        },
        {
            name = "weather-forecast",
            description = "Get weather forecast",
            execute = function(args)
                local r = web.fetch("https://api.weather.com/forecast?city=" .. args.city)
                return r.content
            end
        }
    }
end
```

### 5. Discovery & Registration

- Lua scripts placed in a special directory (e.g., `~/.dass/plugins/`)
- Scanned on startup or via "Reload Scripts" action
- Each script declares what it implements via frontmatter `implements` field
- Registration in DI container dynamically

```csharp
public interface ILuaServicePlugin
{
    string PluginType { get; }  // "ExecutionStage", "MessageExtension", etc.
    Task InitializeAsync(LuaService lua, IServiceProvider services);
}
```

### 6. Security Considerations

- Sandboxed execution (already done via MoonSharp `CoreModules.Preset_SoftSandbox`)
- Explicit permissions declaration in frontmatter
- User confirmation required for certain plugin types

## Integration with Existing System

This naturally extends the meta tool concept. The `MetaToolModule` already handles creation/deletion. We need:

- A `LuaPluginManager` service that discovers and loads plugins
- Registration adapters (`ILuaExecutionStage`, `ILuaMessageExtension`, etc.)
- UI for managing installed script plugins

## Priority

**Low/Experimental** — powerful but complex. Should be built after core systems stabilize.
