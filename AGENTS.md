# dASS - Desktop Assistant (AGENTS.md context)

The project is cross-platform C#/Avalonia application for universal LLM/agentic interactions.

## Agents

The project has **2** separate agentic execution systems:

1. *Chat agents* - agents that live in a chat, triggered by user messages, ordered by consecutive *execution stages*, and cannot execute in parallel.
2. *Agentic tasks* - agents that runs once for doing specific tasks
    - Internal system tasks, such as chat naming, automatic memory recording/retrieval, etc.
    - Explicit invokation via `agent-call` tool or via `dass.agent.call` Lua API
    - Predefined *sub-agents* that live in `.agents/agents/`-like directories

### Directories

```
src/LLMDesktopAssistant/Agents/
    ChatAgentDescriptor.cs - the descriptor/configuration object for the chat agent
    ChatAgentInstance.cs - the agent reference (by agent's GUID) used in execution stages ONLY
    ... other root files used mostly for ChatAgentDescriptor's configuration
    ExecutionStages/
        AgentExecutionStage.cs - abstract base class for execution stages
        AgentPreExecutionContext.cs - context that taken by execution stages to select next agent
        AdaptiveAgentExecutionStage.cs - the adaptive execution stage that uses LLM-based router for selecting next agent
        ...
    Memory/ - agentic memory-related directory (facts/episodic logs)
        IMemoryFactStore.cs
        IMemoryLogStore.cs
        ...
    SubAgents/ - predefined agentic tasks-related files, contains parser, loader, etc.
        SubAgentInfo.cs - the main sub-agent information object, contains name, description, metadata, used tools, skill, memory blocks, inner sub-agents
        ...
    Tasks/
        AgentTask.cs - agentic task object, produced by AgentTaskExecutor
        AgentTaskExecutor.cs - the implementation of IAgentTaskExecutor
        AgentTool.cs - abstract definition of tool, used by agent inside task
        ChatAgentTool.cs - wrapper for chat's ToolInfo that inherits AgentTool
        ...

src/LLMDesktopAssistant/LLM/
    Domain/
        Chat.cs - the chat instance itself, contains a messages sequence and ContextTabs - point of persisted/visual extension
        ChatMessage.cs - abstract base class for messages
        ...
    Services/
        ChatExecutionService.cs - **central** service for chat execution pipeline
        ...
        Agents/
            AgentManagementService.cs - service for chat agents retrieval
            AgentOrderingService.cs - service for getting next chat agent for execution
            ...
        Prompting/
            ChatPromptBuilder.cs - cental service for chat-related prompting, used to build message seqeunce for each chat agent
            ...
        Tools/
            ToolExecutionService.cs - service for chat-related tool execution
            ToolsetBuildingService.cs - service for collecting and building toolset from all sources for each agent
            ...

```

## Dependency Injection

Project uses Microsoft.Extensions.DependencyInjection, paired with own reflection-based registration system. It uses three main scopes:
- `App` - registered with `LLMDesktopAssistant.Services.ServiceAttribute(Type? serviceType = null)` as a singleton, multiple attributes are allowed to register under multiple service types.
- `Chat` - registered with `LLMDesktopAssistant.LLM.Services.ChatServiceAttribute(Type? serviceType = null)` as a scoped type, where single scope = single chat, multiple attributes are also allowed. All app services are avaliable within chat services.
- `WebUI` - registered with `LLMDesktopAssistant.Blazor.Services.WebUIServiceAttribute(Type? serviceType = null, IsScoped = true|false)` as service within ASP.NET web application (scoping is optional). Multiple attributes are not allowed here. All chat services (including app) available for WebUI services.

Sugar attributes:
- `LLMDesktopAssistant.Tools.ToolModuleAttribute(bool chatScoped = true)` used to define inheritants of `LLMDesktopAssistant.Tools.ToolModule` as services. Sugar for `[Chat]Service(typeof(ToolModule))`
- `LLMDesktopAssistant.Scripting.Lua.LuaApiAttribute(bool chatScoped = true)` used to define inheritants of `LLMDesktopAssistant.Scripting.Lua.LuaApiBaseAsync` as services. Sugar for `[Chat]Service(typeof(LuaApiBaseAsync))`

Examples of registering service:
```csharp
using LLMDesktopAssistant.Services;

namespace LLMDesktopAssistant.Agents.Tasks
{
	[Service(typeof(IAgentTaskExecutor))]
	public class AgentTaskExecutor : IAgentTaskExecutor
	{
        ...
    }
}
```

```csharp
namespace LLMDesktopAssistant.Tools.Implementations.Filesystem
{
	[ToolModule] // Chat-scoped by default
	public class FilesystemToolModule : ToolModule
	{
		private readonly IWorkingDirectoryAccessService _fileAccess;
		private readonly IDocumentReadingService _documentReader;

		public FilesystemToolModule(IWorkingDirectoryAccessService fileAccess, IDocumentReadingService documentReader)
        {
            ...
        }

        ...
    }
}
```

### Service configurators

You can define a class that inherits `LLMDesktopAssistant.Services.ServiceConfigurator`. Also put `LLMDesktopAssistant.Services.ServiceConfiguratorAttribute(ServiceScope scope = ServiceScope.App)` attribute on top of it, for example:

```csharp
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Utils;

[ServiceConfigurator(ServiceScope.App)]
public class AppToolModulesConfigurator : ServiceConfigurator
{
	public override void Configure(IServiceCollection services)
	{
		var toolModules = ReflectionUtility.GetTypesWithAttribute<ToolModule, ToolModuleAttribute>();
		foreach (var toolModule in toolModules)
		{
			if (!toolModule.Attribute.ChatScoped)
				services.AddSingleton(typeof(ToolModule), toolModule.Type);
		}
	}
}

[ServiceConfigurator(ServiceScope.Chat)]
public class ChatToolModulesConfigurator : ServiceConfigurator
{
	public override void Configure(IServiceCollection services)
	{
		var toolModules = ReflectionUtility.GetTypesWithAttribute<ToolModule, ToolModuleAttribute>();
		foreach (var toolModule in toolModules)
		{
			if (toolModule.Attribute.ChatScoped)
				services.AddScoped(typeof(ToolModule), toolModule.Type); // Note that chat services are scoped!
		}
	}
}
```

## Localization

Project uses own localization system based on .loc files:

```
// Comment; only exact single line comments are supported, lines that not starts with '//' cannot contain comments

// Locale definition, leave empty for 'iv' (invariant locale)
%locale:

// Namespace, used as prefix for all locale keys in locale file
%namespace: model

// For example, this locale key is 'model.capability.chat'
capability.chat: Chat completions
```

Localization files are located in `src/LLMDesktopAssistant/Localization/Resources/*locale*/*.loc` as an embedded resources and auto-imported by `LLMDesktopAssistant.Localization.LocFileLocalizationManager` (do not worry about that, just put .loc files and it will just work!).

**Important note**: when searching for existing locale files using `fs-grep` - DO NOT PUT POSSIBLE NAMESPACE AS SEARCH PATTERN - or tool will not find anything (namespace is located separately). Example: when searching for `model.capability.chat`, search for `capability.chat` instead. Also notice: file name != namespace (possibly, e.g. tools.loc has `tool` namespace).

### How to use locale keys

Use `LLMDesktopAssistant.Localization.Locale` static facade class for getting locale keys and values:

```csharp
using LLMDesktopAssistant.Localization;

LocaleKeyBase dynamicKey = Locale.GetKey("tool.name.fs-edit");
LocaleKeyBase constKey = Locale.GetConstKey("Edit file"); // Get wrapper around LocaleKeyBase that returns static string without localization

// Three ways to get SAME value
string localized = Locale.Get("tool.name.fs-edit");
localized = dynamicKey.Value;
localized = dynamicKey.RawValue ?? "tool.name.fs-edit";
localized = constKey.Value;
```

Inside AXAML (via `LocExtension`):

```xml
<...
    xmlns:loc="using:LLMDesktopAssistant.Localization"
    ...>

    <!-- Use static string key -->
    <Button Content="{loc:Loc common.save}"/>

    <!-- Use reactive binding to LocaleKeyBase -->
    <TextBlock Text="{loc:Loc {Binding TitleKey}}"/>

</...>
```

## Settings system

Settings are managed by `LLMDesktopAssistant.Settings.SettingsManager` using `SettingsCategory<TObject>` where `TObject : SettingsObject`. Settings are auto-saved on even *deep* changes (using `LLMDesktopAssistant.Utils.ChangeTracker`), and serialized using `System.Text.Json` with string enum conversion and own abstract type resolution (via `LLMDesktopAssistant.Utils.Json.JsonDerivedAttribute(Type baseType, string discriminator)` on implementation/derived types). `SettingsAttribute(string name)` is used to define name for JSON file that will contain configuration.

### Accessing app & chat settings

For accessing app settings - use `LLMDesktopAssistant.Settings.Application.ApplicationSettingsAccessor`, it has `ApplicationSettings` property that returns `LLMDesktopAssistant.Settings.Application.ApplicationSettings`. **Always** use accessor - its good for testing purposes.

For chat settings - inject `LLMDesktopAssistant.LLM.Services.IChatSettingsService`, it has `LLMDesktopAssistant.LLM.Settings.ChatSettings Settings` property.

### Chat & Agent settings inheritance

`ChatSettings` and `AgentDescriptor` can have inherited settings inside each category. `Chat` settings can be inherited from `App` settings, and `Agent` settings can inherit from both. Source generator generates for each "inherited" setting that have `LLMDesktopAssistant.SourceGenerators.InheritedChatSettingAttribute` (for `Chat`) and `LLMDesktopAssistant.SourceGenerators.InheritedChatAgentSettingAttribute` (for `Agent`). Each category must have `SettingsRouteAttribute(string route)` for enable generation. Examples:

```csharp
[SettingsRoute(nameof(ChatAgentDescriptor.Read))]
public partial class AgentReadSettings : AgentSettingsCategoryBase
{
	private AgentReadPermissions _readPermissions = ...;
	/// <summary>
	/// The permissions that determine what the agent can read.
	/// </summary>
	[InheritedChatAgentSetting]
	public AgentReadPermissions ReadPermissions
	{
		get => _readPermissions;
		set => SetProperty(ref _readPermissions, value);
	}

    ...
}
```

Generator will generate for each inherited property:
```csharp
partial class AgentReadSettings
{
	public global::LLMDesktopAssistant.LLM.Settings.ChatSettingsInheritanceLevel ReadPermissionsInheritance { get; set; }

	public global::LLMDesktopAssistant.Agents.AgentReadPermissions GetEffectiveReadPermissions(global::LLMDesktopAssistant.LLM.Settings.ChatSettings chatSettings)
	{
		...
	}

	public void SetEffectiveReadPermissions(global::LLMDesktopAssistant.LLM.Settings.ChatSettings chatSettings, global::LLMDesktopAssistant.Agents.AgentReadPermissions value)
	{
		...
	}
}
```

`Chat` settings have same generated methods signature, except for removed `chatSettings` parameter.

### Using inherited settings

```csharp
ChatAgentDescriptor agent = ...;
ChatSettings chatSettings = ...; 
var readPerms = agent.Read.GetEffectiveReadPermissions(chatSettings);

var skillSources = chatSettings.Skills.GetEffectiveSources();

chatSettings.Skills.PropertyChanged += (s, e)
{
    if (e.PropertyName == nameof(ChatSkillSettings.SourcesInheritance))
    {
        // Catch inheritance level changes
    }
};
```

Note: `ChatSettingsInheritanceLevel` enum have `{ Application, Profile, Agent }` values.

## MVVM

For most ViewModels this project uses `LLMDesktopAssistant.MVVM.ViewModelBase` and `LLMDesktopAssistant.NotifyPropertyChanged` (parent for `ViewModelBase`) classes. Main usage pattern: `ViewModelBase` used for ViewModels, and `NotifyPropertyChanged` used for parts/items inside ViewModels. Project uses reflection-base view locator, VM's can be bound to views used `LLMDesktopAssistant.MVVM.ViewModelForAttribute(Type targetView)`:

```csharp
using LLMDesktopAssistant.MVVM;

[ViewModelFor(typeof(SomeView))]
public class SomeViewModel : ViewModelBase
{
    private bool _enabled;
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (SetProperty(ref _enabled, value))
                RaisePropertyChanged(nameof(EnabledChanged)); // Optional: raise changed event on depended property
        }
    }

    private RangeObjservableCollection<int> _ids = [];
    public RangeObjservableCollection<int> Ids
    {
        get => _ids;
        set => _ids.Reset(value); // Reset the collection with new items instead of setting the entire property
    }
}
```

### Dialogs

You can use `LLMDesktopAssistant.Controls.Dialogs.DialogManager` for showing dialogs:

```csharp
using LLMDesktopAssistant.Controls.Dialogs;
using LLMDesktopAssistant.Localization;

var dialog = new ConfirmDialogViewModel
{
	Title = LocalizationManager.LocalizeStatic("settings.memory.delete.title"),
	Description = LocalizationManager.LocalizeStatic("settings.memory.delete.confirm"),
	ConfirmText = LocalizationManager.LocalizeStatic("settings.memory.delete.action"),
	CancelText = LocalizationManager.LocalizeStatic("common.cancel"),
	IsDanger = true
};

var result = await DialogManager.ShowDialogAsync(dialog);
var confirmed = (bool)result!;

// Or inside the dialog's VM
DialogManager.CloseDialog(true);
```

## Useful utilities

These are located in `LLMDesktopAssistant.Utils`:

- `RangeObservableCollection` - thread-safe alternative to `ObservableCollection` that can be used for settings and MVVM, is NEVER raises `CollectionChanged` event without `OldItems` and `Newitems`, which is convenient when `Reset` event is unwanted to deal with. **Important notice**: use `Reset(IEnumerable<T>)` method for setter inside reactive objects to reset the *collection* instead of resetting entire *property*.
- `ReadOnlyObservableCollection` - wrapper around any of `IReadOnlyList<T>`, `INotifyCollectionChanged` or `INotifyPropertyChanged` (every implementaion is optional, but at least on must be implemented).
- `AsyncCache` - thread-safe async dictionary with cleanup intervals and sliding expiration time.
- `ChangeTracker` - deep tracker for one specific reactive object, use `ChangeTracker.Untracked` attribute on properties to prevent deep observation for complex objects (`Task` for example).
- `LLMDesktopAssistant.Disposable` - base class for all disposable objects, you can create direct instance of it with provided `Action` (e.g. `new Disposable(() => ...)`).

## Important notes

The info given in this context document is ACTUAL and you are not needed to observe files by self (unless REALLY needed), even if you miss the right signatures, the `dotnet build` will show all the errors - it's better "invent" signatures and fix errors later, because you will spend less tokens!

So, the GOOD example:
```
Okay, I will just use `GetEffectiveReadPermissions(_chatSettings.ChatSettings)`, i don't need to observe the real generator and spend input tokens + one request.
```

The BAD example:
```
Let's view the `src/LLMDesktopAssistant.SourceGenerators/InheritedSettingsGenerator.cs`, who knows if user is right?
*fs-read_entry call*
The user was right, it works as he said...
```

Also, observe the MINIMAL number of files that really needed for understanding behaviour and signatures.

## Building project

DO NOT build the entire solution! Build the each project separately instead (but desktop project should be built inside the temporary directory):

```powershell
dotnet build 'src/LLMDesktopAssistant/LLMDesktopAssistant.csproj'
dotnet build 'src/LLMDesktopAssistant.Desktop/LLMDesktopAssistant.Desktop.csproj' -p:UseArtifactsOutput=true -p:ArtifactsPath='$env:TEMP\dass-temp-build\'
```

## Project memory

Use specified memory block to store completed work inside the epizodic logs after the completed session. You also can get last 5-10 logs to view the actual state of the project.