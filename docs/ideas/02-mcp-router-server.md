# dASS MCP Router & Server

> **Status:** Proposal  
> **Priority:** High  
> **Goal:** Make dASS both an MCP client (consume external tools) AND an MCP server (expose all tools to any MCP client)

## Current State

dASS already has **MCP client** support:

```
External MCP Server  ──→  dASS (MCP Client)
                          ├── MCPManager
                          ├── MCPToolModule → registers tools
                          └── LLM uses those tools
```

But dASS cannot **serve** its tools to other MCP clients.

## Goal: dASS as MCP Hub / Router

```
Any MCP Client  ──→  dASS (MCP SERVER + ROUTER)
  (Claude Desktop,   │
   VS Code,          ├── 53+ native tools
   Cursor,           ├── ∞ meta tools (Lua/Python)
   Continue.dev,     ├── ∞ MCP client tools (transit)
   any LLM client)   └── Lua API
```

## Architecture

```
                      ┌──────────────────────────────────┐
                      │        MCP CLIENTS               │
                      │  (Claude Desktop, VS Code,       │
                      │   Cursor, Continue.dev, etc.)    │
                      └──────────────┬───────────────────┘
                                     │ MCP protocol (HTTP/SSE or Stdio)
                                     ▼
┌────────────────────────────────────────────────────────┐
│                  dASS MCP ROUTER                        │
│                                                        │
│  ┌──────────────────────────────────────────────────┐  │
│  │           MCP SERVER HOST                         │  │
│  │                                                   │  │
│  │  Transports:                                      │  │
│  │  - HTTP/SSE (AspNetCore, port 51234)              │  │
│  │  - Stdio (stdin/stdout, for process-based tools)  │  │
│  │                                                   │  │
│  │  Tool Registration:                               │  │
│  │  - Reads from IToolsetCacheService                │  │
│  │  - Exposes ALL tools as MCP tools                 │  │
│  └──────────────────────────────────────────────────┘  │
│                                                        │
│  ┌──────────────────────────────────────────────────┐  │
│  │         INTERNAL TOOL REGISTRY                    │  │
│  │                                                   │  │
│  │  ┌──────────┐  ┌──────────┐  ┌────────────────┐  │  │
│  │  │  Native  │  │  Meta    │  │  MCP Client    │  │  │
│  │  │  53 tools│  │  ∞ tools │  │  ∞ tools       │  │  │
│  │  └──────────┘  └──────────┘  └────────────────┘  │  │
│  └──────────────────────────────────────────────────┘  │
│                                                        │
│  ┌──────────────────────────────────────────────────┐  │
│  │         MCP CLIENT MANAGER (existing)             │  │
│  │                                                   │  │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐       │  │
│  │  │ GitHub  │  │ Filesys │  │ Database │  ...    │  │
│  │  │ MCP     │  │ MCP     │  │ MCP      │        │  │
│  │  └──────────┘  └──────────┘  └──────────┘       │  │
│  └──────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────┘
```

## Implementation

### NuGet Dependencies

```xml
<PackageReference Include="ModelContextProtocol" Version="1.3.0" />
<PackageReference Include="ModelContextProtocol.AspNetCore" Version="1.3.0" />
```

### MCP Server Host (HTTP/SSE)

```csharp
public static class DassMcpRouterExtensions
{
    public static WebApplication MapDassMcpServer(
        this WebApplication app, 
        IToolsetCacheService toolset)
    {
        app.MapMcpServer("dass-mcp", server =>
        {
            server.ServerInfo = new Implementation 
            { 
                Name = "dASS", 
                Version = "1.0.0" 
            };

            // Register ALL tools from the toolset
            foreach (var (name, tool) in toolset.AvailableTools)
            {
                server.Tools.AddFunction(
                    name: name,
                    description: tool.DescriptionGetter(),
                    parameterTypeInfo: tool.ArgumentSchema,
                    handler: async (JsonNode args, CancellationToken ct) =>
                    {
                        var ctx = ToolExecutionContext.CreateDummy(tool, args, null);
                        var result = await tool.Executor.Invoke(args, ctx, ct);
                        var success = await result.Completion;
                        
                        return new CallToolResponse
                        {
                            Content = [
                                new TextContentBlock(result.ResultContent)
                            ],
                            IsError = !success
                        };
                    }
                );
            }
        });
        
        return app;
    }
}
```

### Stdio Server (for process-based MCP clients)

```csharp
public class DassMcpStdioServer : IAsyncDisposable
{
    private readonly McpServer _server;
    
    public DassMcpStdioServer(IToolsetCacheService toolset)
    {
        var builder = new McpServerBuilder(
            new Implementation { Name = "dASS", Version = "1.0.0" }
        ).AddStdioTransport();
        
        foreach (var (name, tool) in toolset.AvailableTools)
        {
            builder.Tools.AddFunction(
                name, 
                tool.DescriptionGetter(), 
                tool.ArgumentSchema, 
                // Handler same as above
            );
        }
        
        _server = builder.Build();
        _ = _server.RunAsync();
    }
}
```

### Launch in Program.cs

```csharp
// Desktop app startup
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMcpServer();

var app = builder.Build();
app.MapDassMcpServer(toolsetCache);
await app.RunAsync("http://localhost:51234");
```

## MCP Client Connection (Claude Desktop config)

```json
{
  "mcpServers": {
    "dass": {
      "type": "http",
      "url": "http://localhost:51234/mcp"
    }
  }
}
```

Or via Stdio:

```json
{
  "mcpServers": {
    "dass": {
      "command": "C:\\path\\to\\dASS.exe",
      "args": ["--mcp-stdio"]
    }
  }
}
```

## Unified Namespace

When a client connects, it sees ALL tools:

```
# Native dASS tools
web-search
fs-read_entry
fs-grep
calculate
random-coin_flip
crypto.md5
...

# Meta tools (created by LLM)
my-custom-weather
code-analyzer
...

# Connected MCP server tools (transit)
github-mcp-create_repo
github-mcp-list_issues
db-mcp-query
docker-mcp-list_containers
...
```

## Routing Logic

```csharp
// When an MCP tool call arrives:
// 1. Look up the tool in IToolsetCacheService
// 2. If found (native/meta/MCP) → execute via its Executor
// 3. If not found → return error

// This is already handled by ToolsetCacheService
// because MCPToolModule registers remote tools
// in the same cache as native tools!
```

## Benefits

- **✅ Single endpoint** — one URL, all tools
- **✅ Zero-config routing** — just register tools, they're automatically exposed
- **✅ Transit** — external MCP servers' tools are available to all clients
- **✅ Pipeline compatible** — JSON repair, macros, previews work for ALL tools
- **✅ No vendor lock-in** — use Claude, VS Code, Cursor, or any MCP client
- **✅ Hot-reload** — meta tools created by LLM are immediately available via MCP

## Configuration UI

- **Enable/disable** MCP server
- **Choose port** (default: 51234)
- **Choose transport** (HTTP, Stdio, or both)
- **Access control** (optional password/token)
- **View connected clients**
