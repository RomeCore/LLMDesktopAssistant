# Web Search: Engine Priority, Premium APIs & Language-Aware Sorting

**Status:** Proposal  
**Author:** Architectural Analysis  
**Date:** 2026-07-15

---

## 1. Motivation

Current `WebSearchToolModule` uses **SearXSharp** with 80+ engines registered in `SearXSharpConfigurator`, but **all engines are treated equally** — they run in parallel, results are merged without hierarchy. This leads to several problems:

| Problem | Example |
|---------|---------|
| **Chinese results everywhere** | Baidu, Sogou, BiliBili results appear even for English queries |
| **Low-quality results first** | Torrent sites, obscure forums appear before premium API results |
| **No incentive for paid APIs** | Users pay for Google/Brave API but their results get mixed with free scrapers |
| **No graceful degradation** | If premium API fails, system doesn't fall back gracefully |
| **No user control** | Can't say "use only paid APIs" or "exclude Chinese engines" |

---

## 2. Proposed Architecture

### 2.1 Engine Priority Levels

```csharp
public enum EnginePriority
{
    /// <summary>
    /// Paid API-based engines (Google Custom Search, Brave Search, Tavily, etc.).
    /// Fast, reliable, no CAPTCHAs, but cost money per query.
    /// </summary>
    Premium = 0,

    /// <summary>
    /// Free but reliable scraping engines (Google scrape, Bing, DuckDuckGo, Brave scrape).
    /// Good quality, widely available, but may have rate limits.
    /// </summary>
    Reliable = 1,

    /// <summary>
    /// General-purpose engines with decent quality (Yahoo, Qwant, Startpage, Mojeek).
    /// Slower, less accurate, but free.
    /// </summary>
    Normal = 2,

    /// <summary>
    /// Specialized or unstable engines (torrents, niche forums, slow APIs).
    /// Use only when other priorities return insufficient results.
    /// </summary>
    Fallback = 3,

    /// <summary>
    /// Regional engines restricted to specific languages/regions (Baidu → zh, Yandex → ru, Naver → ko).
    /// Excluded unless the query language matches.
    /// </summary>
    Regional = 4
}
```

### 2.2 Engine Metadata

Each engine needs additional metadata for intelligent selection:

```csharp
public interface IEngineMetadata
{
    EnginePriority Priority { get; }
    string[] SupportedLanguages { get; }    // e.g. ["en", "zh"] — empty means all
    string? Region { get; }                 // "CN", "RU", "KR", etc.
    string[] SupportedCategories { get; }   // which search categories it supports
    bool RequiresApiKey { get; }
    double QualityWeight { get; }           // 0.0–1.0 for result ranking
    TimeSpan PreferredTimeout { get; }      // per-engine timeout
}
```

### 2.3 Premium API Engines to Add

| Engine | Cost | Quality | Implementation |
|--------|------|---------|----------------|
| **Google Custom Search** | $5/1k queries | ⭐⭐⭐⭐⭐ | `GoogleCustomSearchEngine` |
| **Brave Search API** | Free 2k/mo, then $5/10k | ⭐⭐⭐⭐⭐ | `BraveSearchApiEngine` |
| **Tavily** | Free 1k/mo, then $10/5k | ⭐⭐⭐⭐ (AI-optimized) | `TavilyEngine` |
| **SerpAPI** | $50/mo 5k queries | ⭐⭐⭐⭐⭐ (Google results) | `SerpApiEngine` |
| **Bing Search API** | $7/1k transactions | ⭐⭐⭐⭐ | `BingSearchApiEngine` |
| **Jina** | Free 1M tokens | ⭐⭐⭐⭐ (AI + search) | `JinaApiEngine` |
| **You.com API** | Free 100 queries | ⭐⭐⭐ | `YouComEngine` |

---

## 3. Search Flow

### 3.1 Engine Selection Algorithm

```
Input: query, language, category, priority, excludeEngines, maxResults

1. Language Detection
   - If language == "auto", auto-detect from query text
   - Determine region: en→US, zh→CN, ru→RU, etc.

2. Engine Filtering
   - Exclude Regional engines if language doesn't match their region
   - Exclude engines not supporting the requested category
   - Exclude explicitly excluded engines
   - Apply priority filter (premium/reliable/normal/fallback/all)

3. Engine Prioritization
   - Group by Priority (Premium → Reliable → Normal → Fallback → Regional)
   - Within each group, sort by QualityWeight descending
   - Take top N from each group (configurable, e.g. 3 premium + 5 reliable + 3 normal)
   - If premium engines are configured but all fail — fall back to reliable

4. Execution
   - Start premium engines first (they're fastest due to API)
   - Launch reliable engines in parallel
   - After premium/reliable finish, evaluate if more results needed
   - Only start normal/fallback if insufficient results

5. Result Merging
   - Deduplicate by URL (keep result from highest priority engine)
   - Boost premium engine results in ranking
   - Apply language penalty (non-matching language results pushed down)
   - Sort by: Priority → Boost → Score → Position
```

### 3.2 New Search Parameters

```csharp
public async Task<ReactiveToolResult> Search(
    [Description("The query to search by")] string query,
    // ... existing params ...
    
    [Description("Minimum engine priority: premium, reliable, normal, fallback")]
    [Enum(["all", "premium", "reliable", "normal", "fallback"])]
    string minPriority = "all",
    
    [Description("Boost premium engine results to the top")]
    bool prioritizePremium = true,
    
    [Description("Exclude specific engines by name (e.g. 'Baidu', 'Yandex')")]
    string[]? excludeEngines = null,
    
    [Description("Exclude regional engines for specific languages (e.g. exclude Chinese engines for English queries)")]
    bool excludeRegionalForLanguage = true,
)
```

### 3.3 Result Format Enhancement

```markdown
**Search results for "query"** 
╔═══════════════════════════════════════╗
║ 🥇 Premium: Google API (5 results)   ║
║ 🥈 Reliable: Bing (3 results)        ║
║ 🥉 Normal: Qwant (1 result)          ║
╚═══════════════════════════════════════╝

**1. [Title](url)** [🥇 Premium | Google API]
  Content text here...
  *Published: 2026-07-15*
  *Language: en* ✓

**2. [Title](url)** [🥈 Reliable | Bing]
  Content text here...
```

---

## 4. Configuration

### 4.1 SearXSharpConfigurator Changes

```csharp
manager.RegisterEngines([
    // 🥇 Premium
    new GoogleCustomSearchEngine(apiKey, searchEngineId) 
        { Priority = EnginePriority.Premium, QualityWeight = 1.0 },
    new BraveSearchApiEngine(apiKey)
        { Priority = EnginePriority.Premium, QualityWeight = 0.95 },

    // 🥈 Reliable
    SearchEngines.Google(logger) 
        { Priority = EnginePriority.Reliable, QualityWeight = 0.9 },
    SearchEngines.Bing(logger) 
        { Priority = EnginePriority.Reliable, QualityWeight = 0.85 },
    SearchEngines.Brave(logger) 
        { Priority = EnginePriority.Reliable, QualityWeight = 0.8 },

    // 🌏 Regional
    SearchEngines.Baidu(logger) 
        { Priority = EnginePriority.Regional, SupportedLanguages = ["zh"], Region = "CN" },
    SearchEngines.Yandex(logger) 
        { Priority = EnginePriority.Regional, SupportedLanguages = ["ru", "tr"], Region = "RU" },

    // 🥉 Normal
    SearchEngines.Yahoo(logger) 
        { Priority = EnginePriority.Normal, QualityWeight = 0.6 },
    SearchEngines.Qwant(logger) 
        { Priority = EnginePriority.Normal, QualityWeight = 0.5 },

    // 📦 Fallback
    SearchEngines.Reddit(logger) 
        { Priority = EnginePriority.Fallback, QualityWeight = 0.3 },
    SearchEngines._1337x(logger) 
        { Priority = EnginePriority.Fallback, QualityWeight = 0.1 },
]);
```

### 4.2 Premium API Key Management

```csharp
// Store API keys securely
public class PremiumApiKeyStore
{
    // Keys stored in encrypted config (appsettings.json or OS keychain)
    public string? GoogleApiKey { get; set; }
    public string? GoogleSearchEngineId { get; set; }
    public string? BraveApiKey { get; set; }
    public string? TavilyApiKey { get; set; }
    public string? BingApiKey { get; set; }
    
    public bool HasAnyPremium => GoogleApiKey != null || BraveApiKey != null 
        || TavilyApiKey != null || BingApiKey != null;
}
```

### 4.3 UI Settings Page

New settings page under "Web Search" with:
- Toggle: "Use Premium APIs" (enable/disable all paid engines)
- Per-engine toggle: enable/disable individual engines
- Priority slider per engine (drag to reorder)
- API key input fields for premium engines
- "Test API Key" button for each premium engine
- "Regional Engine Filtering" toggle (exclude regional engines by language)
- "Min Priority" dropdown for search tool default

---

## 5. Implementation Plan

### Phase 1: Priority Infrastructure
1. Add `EnginePriority` enum to SearXSharp or dASS
2. Add `IEngineMetadata` interface
3. Add priority/region/language properties to existing engines
4. Implement engine filtering & sorting in `WebSearchToolModule`
5. Add language-aware regional engine exclusion

### Phase 2: Premium API Engines
1. Create `IPremiumSearchEngine` marker interface
2. Implement `GoogleCustomSearchEngine`
3. Implement `BraveSearchApiEngine`
4. Implement `TavilyEngine`
5. Add `PremiumApiKeyStore` and wire into DI
6. Update `SearXSharpConfigurator` with optional premium engines

### Phase 3: Smart Result Merging
1. Deduplication with priority-aware keeper (keep premium result)
2. Priority boost in result ranking
3. Language relevance scoring
4. Graceful degradation (fallback when premium fails)

### Phase 4: UI & User Control
1. Settings page for engine configuration
2. API key management in UI
3. Per-agent engine preferences
4. Search result badges (Premium/Reliable/Fallback)

---

## 6. Open Questions

1. Should premium API keys be stored in:
   - Encrypted app settings?
   - OS keychain (Windows Credential Manager, macOS Keychain)?
   - User-provided environment variables?

2. How to handle rate limiting for free scraping engines when premium APIs are not configured?

3. Should we add a "quick mode" that uses only premium/reliable engines for faster results?

4. How to handle billing awareness — notify user when approaching API quota limits?

5. Should regional engine filtering be opt-in ("only include Chinese engines when language is zh") or opt-out ("exclude Chinese engines for non-zh queries")?

---

## 7. Future Extensions

- **Usage analytics**: Track which engines return best results per query type
- **Auto-tuning**: Adjust engine priority based on historical success rates
- **Hybrid mode**: Use premium API results to verify/rank free scraping results
- **Per-region result boosting**: Boost local results (e.g., Google.ru for Russian queries)
- **Machine learning relevance**: Train a model to score result relevance based on user feedback
