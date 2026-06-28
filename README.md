# dASS — Desktop Assistant

**dASS (Desktop Assistant)** is a powerful, multi-platform desktop application built with **Avalonia UI** and **.NET 10** that provides an intelligent LLM-powered assistant with a rich set of tools, multi-agent collaboration, MCP (Model Context Protocol) support, and a web-based chat UI for multi-user chatting.

---

## ✨ Features

### 🧠 Multi-Agent System
- **Multiple specialized agents** with individual configurations
- **Agent execution strategies**: Sequential, Random, Adaptive, Mention-Only, and Repeatable
- **Agent read permissions** — control what each agent can see in the conversation
- **Agent behavior sliders** — fine-tune personality traits like creativity, conciseness, formality, etc.

### 🛠️ Extensible Tool System
- **Filesystem operations** — read, write, search, replace, copy, delete files and directories
- **Web requests & search** — fetch URLs, search the web, download files
- **Document reading** — PDF, DOCX, PPTX
- **Image description** — describe images using vision models
- **Mathematics** — execute mathematical calculations
- **Random** — dice rolls, random numbers, GUIDs, list shuffling
- **Human-in-the-Loop** — file pickers, confirmation dialogs, choice selection
- **Time utilities** — get current time, wait/delays
- **Python execution** — Python execution in isolated or shared environments
- **Lua scripting** — scriptable via AsyncLua (async/await Lua interpreter with full dASS API bindings)
- **Meta tools** — dynamic tools that can be created by LLM using Python

### 🌐 MCP Support (Model Context Protocol)
Built-in support for the **Model Context Protocol** (`ModelContextProtocol` library), enabling connections to external MCP servers and providing tools from those servers to the LLM.

### 🌍 Web Chat UI (Blazor)
- Built-in **Blazor-based Web UI** that can be hosted on a local endpoint
- Optional password protection
- Remote user management with hashed passwords

### 💬 Rich Chat Interface
- **Markdown rendering** with syntax highlighting
- **Message branching** — branch off conversations at any point
- **Message editing and regeneration**
- **Token counting & usage statistics**
- **Chat summarization**
- **Attachments** — add context from files, images, and documents
- **Multiple chat sessions** with storage (LiteDB)

### 🔧 Customization
- **Prompt manager** — edit system prompts, user prompts, personas, specializations, and components via LLT (LLM Template) files
- **Behaviour sliders** — adjust assistant behaviour on the fly
- **Personas & specializations** — predefined role-based configurations
- **Extensive settings UI** — model settings, agent settings, MCP settings, execution stages, user preferences

### 📱 Cross-Platform
Built with **Avalonia UI** targeting:
- **Desktop** (Windows, macOS, Linux)
- **Browser** (WebAssembly)
- **Android**
- **iOS**
- **Blazor** (multi-user Web UI hosted from desktop)

---

## 🏗️ Project Structure

```
LLMDesktopAssistant.sln
├── LLMDesktopAssistant/              # Core shared library (Avalonia)
│   ├── Agents/                       # Agent system & execution stages
│   ├── Calculation/                  # Math expression evaluator
│   ├── Controls/                     # Custom Avalonia controls
│   ├── Converters/                   # Value converters
│   ├── Data/                         # Database layer (LiteDB)
│   ├── LLM/
│   │   ├── Domain/                   # LLM domain models (Chat, Messages, etc.)
│   │   ├── MVVM/                     # Chat UI: view models & views
│   │   │   ├── Messages/             # Message rendering components
│   │   │   ├── Settings/             # Settings views (Agents, Models, MCP, etc.)
│   │   │   ├── Attachments/          # Attachment manager
│   │   │   └── Additional/           # Additional context panels
│   │   ├── Services/                 # Chat services (execution, storage, summarization, etc.)
│   │   │   ├── Agents/               # Agent management & ordering
│   │   │   ├── Attachments/          # Attachment & document reading
│   │   │   └── Tools/                # Tool execution & toolset management
│   │   └── Settings/                 # Chat configuration models
│   ├── Localization/                 # i18n (English & Russian)
│   ├── MCP/                          # Model Context Protocol integration
│   ├── MVVM/                         # Main window, view locator
│   ├── Prompting/                    # Template-driven prompt system (LLT)
│   │   └── Resources/                # LLT template files
│   ├── Scripting/                    # Lua scripting integration
│   ├── Services/                     # Core DI service registry
│   ├── Settings/                     # Settings framework
│   ├── Speech/                       # Speech recognition & synthesis
│   ├── Styles/                       # Avalonia styles & themes
│   ├── Tools/                        # Tool module framework
│   │   ├── Implementations/          # Built-in tool modules
│   │   └── Forms/                    # Form-based tool UIs
│   ├── UIExtensions/                 # Code block & message UI extensions
│   ├── Utils/                        # Utilities (caching, reflection, collections)
│   └── WebUI/                        # Blazor-based Web Chat UI
├── LLMDesktopAssistant.Desktop/      # Desktop launcher (Windows/Linux/macOS)
├── LLMDesktopAssistant.Android/      # Android launcher
└── LLMDesktopAssistant.Blazor/       # Blazor UI Server project
```

---

## 🚀 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- An LLM model endpoint (e.g., Ollama, OpenAI-compatible API)

### Configuration

The application uses a settings system stored in the configured directories (see `Directories.cs`). You can configure:

- **Agents** — create and configure multiple agents with different capabilities
- **MCP servers** — connect to external MCP servers for additional tools
- **Web UI** — enable the Blazor web interface on a local endpoint
- **Prompts** — customize system prompts, personas, and specializations

---

## 🧩 Technology Stack

| Technology | Purpose |
|---|---|
| **.NET 10** | Runtime & SDK |
| **Avalonia UI 12** | Cross-platform UI framework |
| **CommunityToolkit.Mvvm** | MVVM pattern |
| **RCLLM** | LLM client library |
| **ModelContextProtocol** | MCP protocol support |
| **LiteDB** | Local embedded database |
| **Serilog** | Logging |
| **Markdig** | Markdown parsing |
| **LiveMarkdown.Avalonia** | Rich markdown rendering |
| **AsyncLua** | Lua scripting (async/await, integrated dASS API) |
| **Whisper.net** | Speech recognition |
| **System.Speech** | TTS on Windows |
| **NAudio** | Audio playback |
| **SixLabors.ImageSharp** | Image processing |
| **PdfPig** | PDF document reading |
| **DocumentFormat.OpenXml** | DOCX/PPTX reading |
| **AngleSharp** | HTML parsing |
| **FluentAvaloniaUI** | Fluent design controls |
| **Material.Icons.Avalonia** | Material Design icons |
| **LLTSharp** | LLM Template engine |

---

## 📜 License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

Copyright © 2026 **RomeCore**
