# Multi-Language Meta Tools (External Process Execution)

> **Status:** Proposal  
> **Priority:** Medium  
> **Goal:** Support meta tools written in any language (Node.js, Ruby, Perl, R, Julia, PHP, Deno, Bun, etc.) by running them as external processes.

## Current State

dASS supports 2 languages for meta tools:

| Language | Engine | Execution |
|----------|--------|-----------|
| **Lua** | `LuaMetaToolEngine` | In-process (MoonSharp) |
| **Python** | `PythonMetaToolEngine` | External process (Desktop only) |

## Proposed Expansion

| Language | Engine | Command | Status |
|----------|--------|---------|--------|
| Python | `PythonMetaToolEngine` | `python script.py` | ✅ Existing |
| Lua | `LuaMetaToolEngine` | In-process (MoonSharp) | ✅ Existing |
| **Node.js** | `NodeJsMetaToolEngine` | `node script.js` | ➕ New |
| **Ruby** | `RubyMetaToolEngine` | `ruby script.rb` | ➕ New |
| **Perl** | `PerlMetaToolEngine` | `perl script.pl` | ➕ New |
| **R** | `RMetaToolEngine` | `Rscript script.r` | ➕ New |
| **Julia** | `JuliaMetaToolEngine` | `julia script.jl` | ➕ New |
| **PHP** | `PHPMetaToolEngine` | `php script.php` | ➕ New |
| **Deno** | `DenoMetaToolEngine` | `deno run script.ts` | ➕ New |
| **Bun** | `BunMetaToolEngine` | `bun script.js` | ➕ New |
| **PowerShell** | `PowerShellMetaToolEngine` | `pwsh script.ps1` | ➕ New |

## Architecture

### Base Class for External Process Engines

```csharp
/// <summary>
/// Base class for meta tool engines that run as external processes.
/// Handles script generation, temporary file management, process execution,
/// and result collection.
/// </summary>
public abstract class ProcessMetaToolEngineBase : IMetaToolEngine
{
    // Language metadata
    public abstract ScriptLanguageType Language { get; }
    public abstract string FileExtension { get; }        // ".js", ".rb", ".pl"
    public abstract string ExecutableName { get; }       // "node", "ruby", "perl"
    public abstract string ExecutableArgs { get; }       // "{script}" or "-e {code}"
    
    // Templates for LLM
    public abstract string ExampleArgs { get; }
    public abstract string ExampleCode { get; }
    
    // Frontmatter delimiters (for meta tool serialization)
    public abstract string FrontmatterStart { get; }     // "/*", "#---", "=begin"
    public abstract string FrontmatterEnd { get; }       // "*/", "#---", "=cut"
    
    // Script building
    protected abstract string BuildScript(JsonNode args, string userCode);
    
    // Serialization / Deserialization
    public MetaTool Deserialize(string fileContent, string name) { /* common logic */ }
    public string Serialize(MetaTool tool) { /* common logic */ }
    
    public Func<JsonNode, ToolExecutionContext, CancellationToken, Task<ReactiveToolResult>> CreateExecutor(MetaTool tool)
    {
        return async (args, context, ct) =>
        {
            // 1. Build full script with injected args
            var code = BuildScript(args, tool.ExecutionCode);
            
            // 2. Write to temp file
            var tempFile = Path.Combine(
                Path.GetTempPath(), 
                $"dass_meta_{Guid.NewGuid()}{FileExtension}");
            await File.WriteAllTextAsync(tempFile, code, Encoding.UTF8, ct);
            
            try
            {
                // 3. Execute process
                var result = await ShellExecutor.ExecuteProcessAsync(
                    ExecutableName,
                    ExecutableArgs.Replace("{script}", tempFile),
                    workDir: context.Chat.Settings.Environment.GetWorkingDirectory(),
                    ct);
                
                // 4. Build result
                var output = new StringBuilder(result.StdOut);
                if (!string.IsNullOrEmpty(result.StdErr))
                    output.AppendLine().AppendLine("Errors:").Append(result.StdErr);
                
                return ReactiveToolResult.Create(
                    result.Success, 
                    output.ToString().TrimEnd());
            }
            finally
            {
                File.Delete(tempFile);
            }
        };
    }
}
```

### Example Implementation: Node.js

```csharp
[Service(ServiceType = typeof(IMetaToolEngine))]
public class NodeJsMetaToolEngine : ProcessMetaToolEngineBase
{
    public override ScriptLanguageType Language => ScriptLanguageType.NodeJS;
    public override string FileExtension => ".js";
    public override string ExecutableName => "node";
    public override string ExecutableArgs => "{script}";
    
    public override string ExampleArgs => """{"location": "New York"}""";
    public override string ExampleCode => """
        const url = `https://api.weather.com/.../${tool_args.location}`;
        const res = await fetch(url);
        const data = await res.json();
        console.log(`Temperature: ${data.temp}°C`);
        """;
    
    public override string FrontmatterStart => "/*";
    public override string FrontmatterEnd => "*/";

    protected override string BuildScript(JsonNode args, string userCode)
    {
        var json = args.ToJsonString();
        return $"const tool_args = {json};\n{userCode}";
    }
}
```

### Example Implementation: Ruby

```csharp
[Service(ServiceType = typeof(IMetaToolEngine))]
public class RubyMetaToolEngine : ProcessMetaToolEngineBase
{
    public override ScriptLanguageType Language => ScriptLanguageType.Ruby;
    public override string FileExtension => ".rb";
    public override string ExecutableName => "ruby";
    public override string ExecutableArgs => "{script}";
    
    public override string ExampleArgs => """{"name": "World"}""";
    public override string ExampleCode => """
        puts "Hello, #{tool_args['name']}!"
        """;
    
    public override string FrontmatterStart => "=begin";
    public override string FrontmatterEnd => "=cut";

    protected override string BuildScript(JsonNode args, string userCode)
    {
        var json = args.ToJsonString();
        return $"require 'json'\ntool_args = JSON.parse('{EscapeJson(json)}')\n{userCode}";
    }
}
```

### Registration in DI

```csharp
// In App.axaml.cs or DesktopApp.cs
services.AddSingleton<IMetaToolEngine, NodeJsMetaToolEngine>();
services.AddSingleton<IMetaToolEngine, RubyMetaToolEngine>();
services.AddSingleton<IMetaToolEngine, PerlMetaToolEngine>();
services.AddSingleton<IMetaToolEngine, RMetaToolEngine>();
// etc.
```

`MetaToolModule` already takes `IEnumerable<IMetaToolEngine>`, so new engines are picked up automatically.

## Meta Tool File Format

Each meta tool is stored as a file with YAML frontmatter:

**Node.js:**
```javascript
/*
title: Weather Checker
description: Gets current weather
category: weather
ask_for_confirmation: false
argument_schema: '{"type":"object","properties":{"location":{"type":"string"}},"required":["location"]}'
*/
const url = `https://api.weather.com/.../${tool_args.location}`;
const res = await fetch(url);
const data = await res.json();
console.log(`Temperature: ${data.temp}°C`);
```

**Ruby:**
```ruby
=begin
title: Weather Checker
description: Gets current weather
category: weather
ask_for_confirmation: false
argument_schema: '{"type":"object","properties":{"location":{"type":"string"}},"required":["location"]}'
=cut
require 'json'
url = "https://api.weather.com/.../#{tool_args['location']}"
data = JSON.parse(`curl -s #{url}`)
puts "Temperature: #{data['temp']}°C"
```

## Additional Features

### Sandboxing (Docker)

```csharp
public class DockerSandboxedEngine : IMetaToolEngine
{
    // Runs scripts in isolated Docker containers
    // docker run --rm -i node:20 node /dev/stdin < script.js
}
```

### Timeouts

Each external process should have a configurable timeout (default: 30s).

### Streaming Output

For long-running scripts, output can be streamed line-by-line via `ReactiveToolResult.ResultContentLines`.

### Caching

If the same script with same arguments is called again, return cached result (optional, opt-in per tool).
