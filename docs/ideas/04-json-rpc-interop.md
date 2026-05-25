# JSON-RPC Interop for External Scripts

> **Status:** Proposal  
> **Priority:** Medium  
> **Goal:** Enable bidirectional communication between external script processes and the dASS application via JSON-RPC over a local socket.

## Problem

Currently, external scripts (Python, Node.js, Ruby) are **black boxes**:

```
dASS → [args] → Script Process → [stdout] → dASS
```

The script cannot:
- Update its own status/progress in the UI
- Ask the user questions during execution
- Call other dASS tools
- Log to the application log
- Access chat history or settings

## Solution: Embedded JSON-RPC Server

For each tool execution, dASS starts a **temporary JSON-RPC server** on a dynamic local port. The external script connects to this server and can call methods during execution.

```
┌──────────────────────────────────────┐
│           dASS (C#)                  │
│                                      │
│  ┌──────────────────────────────┐   │
│  │  JsonRpcHost :PORT           │   │
│  │  - TcpListener (127.0.0.1)  │   │
│  │  - RegisterMethod(...)      │   │
│  │  - JsonRpcProcessor         │   │
│  └──────────────────────────────┘   │
│                                      │
│  Injects RPC client code at the      │
│  beginning of the script             │
└──────────────┬───────────────────────┘
               │ localhost:PORT
               │ (TCP or NamedPipe)
┌──────────────▼───────────────────────┐
│  External Script (Python/Node/Ruby)   │
│                                       │
│  // Injected by dASS:                 │
│  import jsonrpc                       │
│  dass = JsonRpcClient("localhost")    │
│                                       │
│  // User code:                        │
│  dass.set_tool_status("Searching...") │
│  dass.set_progress(0.5)               │
│  result = dass.call_tool("web-search"│
│  user_input = dass.prompt_user("?")   │
│  print(result)                        │
└───────────────────────────────────────┘
```

## JSON-RPC Protocol

Standard [JSON-RPC 2.0](https://www.jsonrpc.org/specification) over TCP:

**Request (script → dASS):**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "set_tool_status",
  "params": { "text": "Searching the web..." }
}
```

**Response (dASS → script):**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": null
}
```

## Available RPC Methods

| Method | Description | Parameters | Returns | UI Impact |
|--------|-------------|------------|---------|-----------|
| `set_tool_status` | Update status text | `text: string` | `null` | ✅ Shows in UI |
| `set_tool_icon` | Change icon | `icon: string` (MaterialIconKind) | `null` | ✅ Visual |
| `set_progress` | Set progress bar | `value: number (0.0-1.0)` | `null` | ✅ Progress bar |
| `log` | Send log message | `level: string, text: string` | `null` | ✅ Console/log |
| `prompt_user` | Ask user a question | `question: string` | `string` (answer) | ✅ Dialog |
| `confirm` | Ask for confirmation | `question: string` | `boolean` | ✅ Dialog |
| `choice` | Offer options | `question: string, options: string[]` | `string` (selected) | ✅ Form |
| `call_tool` | Call another tool | `name: string, args: object` | `object` (result) | 🔄 Nested |
| `get_chat_history` | Get conversation | — | `array` | 📜 Context |
| `get_chat_variables` | Get chat vars | — | `object` | 📦 Data |
| `read_file` | Read file | `path: string` | `string` | 📁 FS |
| `write_file` | Write file | `path: string, content: string` | `null` | 📁 FS |
| `execute_lua` | Run Lua snippet | `code: string` | `any` | 🔄 Interop |
| `get_setting` | Get setting | `key: string` | `any` | ⚙️ Config |
| `set_result` | Set partial result | `content: string` | `null` | 📤 Streaming |

## C# Implementation

### JsonRpcHost

```csharp
public class JsonRpcHost : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly Dictionary<string, Func<JsonNode?, Task<JsonNode?>>> _methods = new();
    private readonly CancellationTokenSource _cts = new();
    
    public JsonRpcHost(int port)
    {
        _listener = new TcpListener(IPAddress.Loopback, port);
    }
    
    public void RegisterMethod<T>(string name, Func<T?, Task<JsonNode?>> handler)
    {
        _methods[name] = async (args) => await handler(args is JsonNode jn ? jn.Deserialize<T>() : default);
    }
    
    public void RegisterMethod<T>(string name, Action<T?> handler)
    {
        _methods[name] = (args) => { handler(args is JsonNode jn ? jn.Deserialize<T>() : default); return Task.FromResult<JsonNode?>(null); };
    }
    
    public async Task StartAsync(CancellationToken ct)
    {
        _listener.Start();
        using var registration = ct.Register(() => _cts.Cancel());
        
        while (!_cts.Token.IsCancellationRequested)
        {
            var client = await _listener.AcceptTcpClientAsync(_cts.Token);
            _ = HandleClientAsync(client, _cts.Token);
        }
    }
    
    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        using (client)
        using (var stream = client.GetStream())
        using (var reader = new StreamReader(stream, leaveOpen: true))
        using (var writer = new StreamWriter(stream, leaveOpen: true))
        {
            var requestLine = await reader.ReadLineAsync(ct);
            var request = JsonSerializer.Deserialize<JsonRpcRequest>(requestLine);
            
            var response = await ProcessRequest(request);
            await writer.WriteLineAsync(JsonSerializer.Serialize(response));
        }
    }
}
```

### Integration with MetaToolExecutor

```csharp
// Inside ProcessMetaToolEngineBase.CreateExecutor()
public Func<JsonNode, ToolExecutionContext, CancellationToken, Task<ReactiveToolResult>> CreateExecutor(MetaTool tool)
{
    return async (args, context, ct) =>
    {
        // 1. Start RPC host on free port
        var port = GetFreePort();
        var rpcHost = new JsonRpcHost(port);
        
        // 2. Register methods that modify the ReactiveToolResult
        var reactiveResult = new ReactiveToolResult();
        rpcHost.RegisterMethod("set_tool_status", (string text) => {
            reactiveResult.StatusTitle = text;
        });
        rpcHost.RegisterMethod("set_tool_icon", (string icon) => {
            reactiveResult.StatusIcon = Enum.Parse<MaterialIconKind>(icon);
        });
        rpcHost.RegisterMethod("set_progress", (double value) => {
            reactiveResult.Progress = value;
        });
        // ... more methods
        
        // 3. Start RPC server in background
        _ = rpcHost.StartAsync(ct);
        
        try
        {
            // 4. Inject RPC client code + set DASS_RPC_PORT env var
            var injectedCode = GenerateRpcClientCode(tool.ScriptLanguage, port);
            var fullCode = injectedCode + "\n" + tool.ExecutionCode;
            
            // 5. Execute process with injected code
            var processResult = await ExecuteProcessAsync(
                tool, fullCode, args, context, ct);
            
            // 6. Complete
            reactiveResult.ResultContent = processResult.Output;
            reactiveResult.Complete(processResult.Success);
        }
        finally
        {
            rpcHost.Dispose();
        }
        
        return reactiveResult;
    };
}
```

## Generated Client Code

### Python Client

```python
import socket, json, os, threading

class DassRpcClient:
    def __init__(self):
        self.port = int(os.environ['DASS_RPC_PORT'])
        self._seq = 0
    
    def _call(self, method, params=None):
        self._seq += 1
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            s.settimeout(10)
            s.connect(('127.0.0.1', self.port))
            req = json.dumps({
                "jsonrpc": "2.0",
                "id": self._seq,
                "method": method,
                "params": params or {}
            })
            s.sendall(req.encode() + b'\n')
            resp = s.recv(65536).decode().strip()
            result = json.loads(resp)
            if "error" in result:
                raise Exception(result["error"])
            return result.get("result")
    
    def set_tool_status(self, text):
        return self._call("set_tool_status", {"text": text})
    
    def set_tool_icon(self, icon):
        return self._call("set_tool_icon", {"icon": icon})
    
    def set_progress(self, value):
        return self._call("set_progress", {"value": value})
    
    def prompt_user(self, question):
        return self._call("prompt_user", {"question": question})
    
    def call_tool(self, name, args=None):
        return self._call("call_tool", {"name": name, "args": args or {}})

dass = DassRpcClient()
```

### Node.js Client

```javascript
const net = require('net');

class DassRpcClient {
    constructor() {
        this.port = parseInt(process.env.DASS_RPC_PORT);
        this._seq = 0;
    }
    
    _call(method, params) {
        return new Promise((resolve, reject) => {
            const client = net.connect(this.port, '127.0.0.1', () => {
                const req = JSON.stringify({
                    jsonrpc: "2.0",
                    id: ++this._seq,
                    method,
                    params: params || {}
                });
                client.write(req + '\n');
            });
            client.on('data', data => {
                try {
                    const resp = JSON.parse(data.toString());
                    if (resp.error) reject(new Error(resp.error));
                    else resolve(resp.result);
                } catch (e) { reject(e); }
                client.end();
            });
            client.on('error', reject);
        });
    }
    
    set_tool_status(text) { return this._call('set_tool_status', { text }); }
    set_tool_icon(icon) { return this._call('set_tool_icon', { icon }); }
    set_progress(value) { return this._call('set_progress', { value }); }
    prompt_user(question) { return this._call('prompt_user', { question }); }
    call_tool(name, args) { return this._call('call_tool', { name, args }); }
}

const dass = new DassRpcClient();
```

## Benefits

- **✅ Bidirectional** — scripts can talk back to the app
- **✅ Human-in-the-loop** — `prompt_user()`, `confirm()`, `choice()` from any language
- **✅ UI updates** — status, icon, progress in real-time
- **✅ Tool chaining** — scripts can call other dASS tools
- **✅ Language agnostic** — same `dass.*` API for Python, Node.js, Ruby, etc.
- **✅ No polling** — JSON-RPC over persistent connection
- **✅ Secure** — binds to localhost only, not exposed to network

## Potential Enhancements

- **Named Pipes** instead of TCP for better security on Windows
- **Batched calls** — send multiple RPC calls in one connection
- **Streaming** — long-running responses via chunked transfer
- **Cancellation** — dASS can cancel a running script via RPC
