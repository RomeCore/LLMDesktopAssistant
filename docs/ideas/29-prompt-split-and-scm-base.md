# Реестр решений: разрезание промптинг-конвейера, база видимости и база SCM (v0)

> **Status:** Decision Registry (нормативный для реализации)
> **Дата сеанса:** 2026-09-23
> **Приоритет:** High (ядро промптинга)
> **Теги:** prompting, sections, scm, checkpoints, visibility, quotes, agents

Собран из дизайн-сеанса (grill). Механика дельт, механика компакций и видимость-рефакторинг
(фасеты/пары/идентичность/SelfVisibility) — **следующие** итерации; здесь только база, на которой
они поедут. Пользователей 0 → миграций нет: старые VM удаляются сразу, несовместимость БД допустима.

## 0. Скоуп итерации

1. **База агентной видимости** — старый алгоритм копируется 1:1 за DI-шов; сам рефакторинг — следующая итерация.
2. **База SCM v0** — режимы, чекпоинты, якоря, заморозка/блит заголовка; дельты НЕ выдаются (секции возвращают null).
3. **Разрезание `IChatPromptBuilder`** на сервисы; `system_prompt.llt` удаляется.
4. **Тул-пайплайн не трогаем**: разрешение вызовов и кеш остаются как есть (единственное изменение — инвалидация кеша уезжает в композер).
5. Тесты в этой итерации не пишутся; приёмка — логи + ручные проверки.

## 1. Топология сервисов

| Сервис | Роль |
|---|---|
| `IAgentPromptComposer` (имя рабочее) | Центральный: `Build(agent) → AgentPromptBundle { IReadOnlyList<IMessage> Messages; IReadOnlyList<FunctionTool> Tools }`. Владеет парой (messages + tools), режимной логикой заголовка и инвалидацией toolset-кеша |
| `IAgentEffectiveMessagesProvider` | Источник правды по сообщениям: эффективный набор + чекпоинты |
| `IPromptStateStage` | SCM-стадия: жизненный цикл якоря (только Hybrid); вызывается композером между провайдером и конверсией |
| `IChatMessageQuoteRenderer` | Нейтральная контент-квота сообщения (naming/summarizer/router) |
| `IMessageVisibilityService` | Шов видимости (копия старого алгоритма) |

Конвейер композера:
`invalidate toolset → provider.GetEffectiveMessages(agent) → stage.Process(agent, effective) → header → конверсия сообщений + hooks + summary-строка → bundle`.

- `ChatExecutionService` больше не ходит в `ToolsetCache` (выкидывает оба блока `Invalidate + ValidTools`); разрешение вызовов (`ValidAliasedTools`) остаётся у него, кеш свеж силами композера.
- `IChatPromptBuilder` умирает: `Build` → композер, `RenderSystemPrompt` → секционные extensions, `RenderMessage` → квотер, `ConvertMessage` → удаляется (потребителей не было).
- Потребители на обновление: `ChatExecutionService` (×2), `ChatNamingService`, `ChatSummarizationService`, `AdaptiveAgentExecutionStage`, `AgentPromptSettingsViewModel`, `ChatSettingsViewModel`.

## 2. Effective messages

```csharp
record EffectiveChatContext {
    IReadOnlyList<BranchedMessage> Messages;            // по возрастанию
    IReadOnlyList<EffectiveCheckpoint> Checkpoints;     // активные, по возрастанию позиции
    int LastCutIndex;                                   // производное билдера; -1 если резов нет
}
record EffectiveCheckpoint { ContextCheckpoint Checkpoint; int Index; }
```

- `Messages` — ровно то, что уйдёт в конверсию и хуки; **включает pending-assistant** (канал hook-инъекций: memory digest, будущие дельты). Провайдер чистый: без хуков, конверсии и рендера. Не кеширует — пересчёт на каждый вызов.
- Чекпоинты несём только **активные (применяемые)**: гейты — `DisabledFlags` (маска покрывает все kinds); всё, что старше точки отсечения walk-а, не несётся.
- **Индексы (относительно `Messages`):**
  - cut-чекпоинт → `-1`;
  - не-cut на видимом носителе → эффективный индекс носителя;
  - не-cut на невидимом носителе → индекс ближайшего предшествующего видимого;
  - предшествующих нет → `-1`.

  Пример: `[x x o x o c o x o x]` (c — невидимый носитель) → `2` (третья `x`).
- **Иммунитет:** все чекпоинты несутся вне зависимости от видимости носителя. В текущем коде щит
  иммунен, саммари — нет (проверка после visibility) — **это баг, выравниваем**: проверка чекпоинтов
  ДО `continue` по видимости.
- **Окно раундов:** применяется в Dynamic/Static; **в Hybrid не применяется** (иначе якорь вытесняется
  из набора → rebaseline-шторм). UI-дизейбл настройки — позже.
- Признак «это рез» и `LastCutIndex` — **не свойства данных**: вычисляются внутри билдера
  эффективного контекста; наружу отдаются производные. Форма экспонирования — на уровне билдера
  (единственная точка правды).
- Walk копирует старое поведение 1:1, кроме фикса иммунитета.

## 3. Чекпоинты

- `ContextCheckpoint : AdditionalChatData { ContextCheckpointKind Kind; string? Context; bool IsCompletedAndEnabled; }`
  - `IsCompletedAndEnabled` — замена `SummaryViewModel.Completed`: гейт готовности (саммари
    генерируется) + ручной тумблер «выключен/включён» (выключенный чекпоинт лежит в истории,
    не действует). Применяемость = `IsCompletedAndEnabled && (DisabledFlags & Kind) == 0`.
- `ContextCheckpointKind` — **`[Flags]`**: `Shield`, `Summary`, `ToolCompaction`,
  `ForcedToolCompaction`, `ReasoningCompaction`. (Скаффолдный `ToolCompact` переименовывается в `ToolCompaction`.)
  - **Cut-kinds:** `Shield`, `Summary`. Cut-ность — **не свойство чекпоинта**: вычисляется внутри
    билдера эффективного контекста, не хранится на данных.
  - **Компакции (не-cuts):** `ToolCompaction` — плейсхолдер для результатов тулов (уважает
    `IsCompactible` тул-колла); `ForcedToolCompaction` — то же, но игнорирует `IsCompactible`;
    `ReasoningCompaction` — аналог, **убирает reasoning полностью** (пусто vs плейсхолдер — при
    реализации). Все компакции ломают кеш ⇒ требуют нового якоря; регион не режут.
  - Семантика включительная: чекпоинт покрывает сообщения по свой носитель включительно.
  - Создание/рендер компакций (триггеры, кнопки, штамп `IsCompactible` в момент исполнения) —
    **следующая итерация** (тул-пайплайн не трогаем); в v0 — только kinds и нормативная семантика.
- Миграция: **нет.** `SummaryViewModel`/`ContextShieldViewModel` сносятся сразу; всё живёт в
  `ContextCheckpoint` (вьюхи перецеливаются).
- `<summary>`-строка собирается композером из новейшего активного `Summary`-чекпоинта (`Checkpoint.Context`).

## 4. Секции и заголовок

- Секция = часть системного промпта. `system_prompt.llt` **удаляется**; логика уходит в рендереры
  секций (рендерер сам решает про шаблоны/код).
- `IPromptSection` (недженерик-фасад): `Type StateType; Type DeltaType; int Order;` +
  `CaptureState(agent)` / `RenderState(state)` / `CalculateDelta(...)` / `RenderDelta(delta)`.
  Без `Try*` — несовпадение типа = исключение.
- `PromptSectionBase<TState, TDelta> : IPromptSection` — требует 4 типизированных сервиса через
  `IServiceProvider` в конструкторе и делегирует (сам их не реализует). `StateType/DeltaType` —
  `typeof`, `Order` — abstract.
- Типизированные сервисы (как в скаффолде): `IPromptSectionStateProvider<TState>` (**capture принимает
  агента**), `IPromptSectionStateRenderer<TState>`, `IPromptSectionDeltaProvider<TState,TDelta>`,
  `IPromptSectionDeltaRenderer<TDelta>`.
- `IPromptSectionDescriptor` — **удаляется** (свойства переезжают в `IPromptSection`). Центрального
  хаба нет: потребители работают с `IEnumerable<IPromptSection>` напрямую.
- `PromptSectionExtensions`: `Ordered()` (= `OrderBy(s => s.Order)`), `CaptureStates(agent)`,
  `RenderHeader(states)`, `RenderHeader(agent)`.
  - Склейка: фрагменты по `Order`; текст — непустые фрагменты через **одинарный `\n`**; тулы —
    конкатенация + сортировка по имени; без тримминга (рендереры владеют байтами).
  - Маппинг state↔секция — по точному типу (`GetType() == StateType`); незнакомые состояния
    пропускаются (warn).
- v0-секции: **CorePrompt** (логика старого `system_prompt.llt` как её собственный рендер) и
  **Tools** (состояние — `SerializableToolDefinition[]` в каноническом порядке; текст пустой).
- Дельта-пара секций в v0 — заглушки (`CalculateDelta → null`); форма регистрации — при реализации.
- **Приёмка:** байты dynamic-заголовка до/после рефакторинга идентичны (golden, вручную).

## 5. SCM v0 — режимы, якоря, статик

### 5.1. Настройки

- Новая категория `Context` на `ChatAgentDescriptor` (`AgentContextSettings : AgentSettingsCategoryBase`,
  свой `SettingsRoute`); **выпинывается из `Read`**. `Read` = только настройки мультиагентной(!) видимости.
- В `Context`: `MaxVisibleRounds`, `DisabledFlags` (замена `AllowContextShields`/`AllowSummaries`),
  `PromptMode` (наследуемый, дефолт **Dynamic**), `Snapshot` (**без наследования**, для Static),
  позже — `SelfVisibility`.
- Наследование — по свойствам (как у `ReadPermissions`).
- `PromptContextMode { Dynamic, Hybrid, Static }` — как в скаффолде.

### 5.2. Якорь

```csharp
class PromptStateAnchorMessageData : AdditionalChatData {
    int Id;                              // монотонный id в рамках чата — из счётчика на Chat
    Guid AgentId;                        // per-agent эпохи
    RangeObservableCollection<PromptSectionStateBase> Sections;  // структурные состояния
    SystemPromptSnapshot Snapshot;       // замороженные байты: текст + тул-аррей
}
```

- `IsVisible = false`, не временный (персистится).
- Id-счётчик — отдельный `AdditionalChatData` на `Chat` (авто-инкремент), лениво создаётся при первом якоре.
- `Reason`-поля нет — причина живёт в логах.

### 5.3. Жизненный цикл (Hybrid, на каждый prep)

1. `LastCutIndex` берётся из `EffectiveChatContext`.
2. Живой якорь = новейший среди якорей агента в `Messages` с индексом носителя `> LastCutIndex`.
3. Нет живого → создание: `states = CaptureStates(agent)` → `snapshot = RenderHeader(states)`
   (один атомарный проход) → якорь вешается на `Messages[LastCutIndex + 1]`; дублей не бывает
   (живой якорь на цели нашёлся бы шагом 2). Цель отсутствует / это pending → пропуск до следующего prep.
   Pending композер передаёт стадии явным параметром (понадобится дельтам).
4. Заголовок: **блит `anchor.Snapshot`** (шаблоны в горячем пути не участвуют).

- **Валидность якоря:** индекс носителя `> LastCutIndex` И носитель в `Messages`.
- **Ребазлайн-триггеры:** нет живого якоря (первый вход в Hybrid / якорь потерян) · рез новее якоря ·
  носитель выпал из набора.
- **Инварианты:** append-only (якоря не мутируются/не удаляются; старые — инертные данные); мёртвые
  якоря не используются; изоляция по `AgentId`.
- **Переключения:** dynamic→hybrid — первый якорь лениво на следующем prep; hybrid→dynamic —
  эмиссии прекращаются, объекты инертны.
- **Окно:** в Hybrid `MaxVisibleRounds` не применяется.

### 5.4. Static

- Заголовок = `AgentContextSettings.Snapshot` (per-agent, без наследования). Пусто (только что
  переключились) → ленивый фриз на первом prep.
- Кнопка **Refresh** в новой Context-вьюхе перезамораживает. Diff-превью live-vs-frozen — следующая итерация.
- Регион живёт как в Dynamic (окно активно, чекпоинты режут, якорей нет).
- Персистентность: снапшот переживает рестарт.

### 5.5. `SystemPromptSnapshot`

- Конструкторы вместо required-init: `[BsonCtor] SystemPromptSnapshot(string text,
  ImmutableArray<SerializableToolDefinition> tools)`; доп. ctor `(string text)`;
  `implicit operator SystemPromptSnapshot(string)`.
- `ImmutableArray`/struct-ы в LiteDB — проверено, ок.

### 5.6. Тул-пайплайн

- Не трогаем: разрешение вызовов как сейчас. В Hybrid/Static тул-аррей приходит из снапшота;
  порядок резолва live→frozen→alias→announced→unknown — вместе с дельтами.

## 6. Цитируемый рендер

- `IChatMessageQuoteRenderer` (chat-scoped): `string RenderQuote(BranchedMessage message)`.
- Нейтральная **контент-квота**: имя + время + контент + метаданные вложений (без payload);
  без reasoning; без тул-коллов; без перспективы читателя (хака `previewAgent` нет;
  exposure-зависимость умирает).
- User-like ассистенты → user-цитата. `RawUserMessage` → плейн-текст (латентный краш закрыт).
- Потребители (все три): `ChatNamingService`, `ChatSummarizationService`, `AdaptiveAgentExecutionStage`.
  Итерация/резы/саммари-логика — у потребителей.
- Обоснование: структурно закрывает границу деклассификации саммари (идея 28 §1.8) — на входе нет
  reasoning/вложений/тул-коллов.

## 7. Шов видимости

- `IMessageVisibilityService` (chat-scoped) — копия `AgentMessageVisibility` 1:1;
  `IChatSettingsService`/`IAgentManagementService` инжектятся; методы — `(BranchedMessage, ChatAgentDescriptor)`.
- Потребители: effective-provider, `AutomaticMemoryReader`, `AutomaticMemoryRecorder` (перевод сразу).
  Static-класс удаляется.
- Следующий рефакторинг: `(visible, reason, grantedMask)`, фасеты/пары/идентичность, `SelfVisibility` —
  точка входа здесь.

## 8. Диагностика и приёмка

- Логи стадии (Serilog): переиспользован/создан якорь (+ MessageId/Index), ребейзлайн (обстоятельство),
  источник заголовка (blit/frozen/live).
- PromptDump: вызов переезжает в композер; в payload — SCM-контекст (режим / источник / id якоря);
  включение по флагу, дефолт выкл.
- Приёмка (ручная + логи; тестов нет):
  - Байтовая стабильность: два подряд prep без изменений → идентичный заголовок.
  - Фриз держит: изменения настроек не меняют заголовок (hybrid — до реза, static — до Refresh).
  - Append-only, мёртвые якоря инертны, изоляция агентов.
  - Golden: dynamic-байты до/после рефакторинга идентичны.
  - Персистентность: якоря и static-снапшот переживают рестарт.
- Отложено: SCM Inspector (таймлайн, frozen-vs-live diff, «что знала модель на N»), дельта-инварианты.

## 9. Порядок внедрения

1. **Настройки** — категория `Context`, `PromptMode`, `DisabledFlags`, flags-kinds (+`IsCompletedAndEnabled`), UI.
2. **Чекпоинты** — снос старых VM, `ContextCheckpoint` везде, иммунитет-фикс, гейты.
3. **Шов видимости** — сервис + перевод потребителей.
4. **Effective provider** — вынос walk, `EffectiveChatContext`/`EffectiveCheckpoint`, `LastCutIndex`.
5. **Секции** — `IPromptSection`/база/extensions, CorePrompt+Tools, снос `system_prompt.llt`, превью.
6. **Композер + квотер** — центральный сервис, пересадка потребителей, снос `IChatPromptBuilder`.
7. **SCM-стадия** — якоря, счётчик, `PromptStateStage`, hybrid-блит, static+Refresh, PromptDump, логи.

Шаги 2↔3 взаимозаменяемы. Каждый шаг заканчивается собираемым состоянием.

## 10. Отложено / отклонено / открытые точки

**Отложено:** дельта-канал (эмиссия/рендер/цепочки), механика компакций (триггеры, `IsCompactible`-штамп,
кнопки), diff-превью static, SCM Inspector, дельта-инварианты, видимость-рефакторинг
(фасеты/пары/идентичность/SelfVisibility), тул-резолв-ордер, UI-дизейбл несовместимых с Hybrid настроек.

**Отклонено (не предлагать повторно):** центральный хаб секций (фасад+extensions достаточно) ·
`Id`/дискриминатор на дескрипторе секции · `Try*`-методы на фасаде (исключения вместо) ·
параметр состава у квотера (все трое = контент) · `AnchorReason` · `IsCut` как свойство чекпоинта.

**Открытые точки (при реализации):** форма носителя `ReasoningCompaction` (пусто vs плейсхолдер) ·
форма экспонирования cut-ности из билдера · регистрация дельта-заглушек (открытые генерики vs
опциональный резолв) · точные имена сервисов.

## 11. Точки в коде

| Файл/место | Действие |
|---|---|
| `LLM/Services/Prompting/ChatPromptBuilder.cs`, `IChatPromptBuilder.cs` | Распустить: walk→провайдер, конверсия/хуки→композер, `RenderMessage`→квотер; удалить |
| `LLM/Services/Prompting/AgentMessageVisibility.cs` | → `IMessageVisibilityService` + impl; удалить static |
| `Agents/Memory/AutomaticMemoryReader.cs`, `AutomaticMemoryRecorder.cs` | Инжект шва видимости |
| `LLM/Services/Prompting/` (новое) | `IAgentEffectiveMessagesProvider`, `IAgentPromptComposer`, `IChatMessageQuoteRenderer`, `IPromptStateStage` |
| `Prompting/State/` | `IPromptSection` + `PromptSectionBase` + extensions; снести `IPromptSectionDescriptor`; секции `CorePrompt`, `Tools`; `PromptStateAnchorMessageData` (+`AgentId`, +`Snapshot`); счётчик |
| `Prompting/ContextCheckpoint.cs`, `ContextCheckpointKind.cs` | +`IsCompletedAndEnabled`; flags-kinds (+`ForcedToolCompaction`, +`ReasoningCompaction`; rename) |
| `Prompting/SystemPromptSnapshot.cs` | Конструкторы, `[BsonCtor]`, implicit из string |
| `Agents/Settings/AgentContextSettings.cs`, `AgentReadSettings.cs`, `ChatAgentDescriptor.cs` | Новая категория Context; чистка Read |
| `LLM/MVVM/Settings/...` | Новая вьюха Context (+Refresh); чистка Read-вью; превью через extensions |
| `LLM/MVVM/Additional/Context/...` | Вьюхи Summary/Shield → рендер `ContextCheckpoint` |
| `LLM/Services/ChatExecutionService.cs` | Композер; снять ToolsetCache-блоки |
| `LLM/Services/ChatNamingService.cs`, `ChatSummarizationService.cs`, `Agents/ExecutionStages/AdaptiveAgentExecutionStage.cs` | Квотер |
| `Prompting/Resources/system_prompt.llt` | Удалить (логика → CorePrompt) |
| `LLM/Services/Prompting/PromptDumpService` | Вызов в композер + SCM-контекст |

*Revision history: собран из grill-сеанса 2026-09-23. Нормативен для текущей итерации; при расхождениях
с частными заметками приоритет — у этого файла.*
