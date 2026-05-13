using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.WebSearch;
using LLMDesktopAssistant.WebSearch.Engines;
using RCLargeLanguageModels.Json.Schema;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.Tools.Implementations
{
    /// <summary>
    /// Tool module that provides web search capabilities to the AI assistant
    /// using the built-in search engines (Google, Bing, etc.).
    /// Replaces the external SearXNG dependency with native C# search engine implementations.
    /// Based on SearXNG's engine architecture with multiple parallel search backends.
    /// </summary>
    [ToolModule]
    public class WebSearchToolModule : ToolModule
    {
        private readonly SearchEngineManager _searchManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSearchToolModule"/> class.
        /// </summary>
        /// <param name="searchManager">The search engine manager that orchestrates all registered engines.</param>
        public WebSearchToolModule(SearchEngineManager searchManager)
        {
            _searchManager = searchManager;

            AddTool(SearchWebAsync,
                new ToolInitializationInfo
                {
                    Name = "web-search",
                    Description = "Search through the web using query. Returns results from multiple search engines (Google, Bing, etc.).",
                    Category = "web"
                });

            AddTool(SearchImagesAsync,
                new ToolInitializationInfo
                {
                    Name = "web-search-images",
                    Description = "Search for images on the web. Returns image URLs, thumbnails, and related metadata.",
                    Category = "web"
                });

            AddTool(SearchNewsAsync,
                new ToolInitializationInfo
                {
                    Name = "web-search-news",
                    Description = "Search for news articles on the web. Returns recent news with sources and publication dates.",
                    Category = "web"
                });
        }

        /// <summary>
        /// Performs a general web search across all available search engines.
        /// Results are formatted as markdown suitable for LLM consumption.
        /// </summary>
        /// <param name="query">The search query.</param>
        /// <param name="page">The page number (1-based, default: 1).</param>
        /// <param name="language">Language code (e.g., "en", "ru", "auto" for automatic detection).</param>
        /// <param name="timeRange">Time range filter: "day", "week", "month", "year", or empty for no filter.</param>
        /// <param name="safeSearch">Safe search level: "none", "moderate", "strict".</param>
        /// <param name="maxResults">Maximum number of results to return (default: 10, max: 50).</param>
        /// <returns>A markdown-formatted string with search results.</returns>
        public async Task<ToolResult> SearchWebAsync(
            [Description("The query to search by")] string query,
            [Description("The page number to return results for (1-based)"), Range(1, 10)] int page = 1,
            [Description("Language code (auto, en, ru, etc.)")] string language = "auto",
            [Description("Time range filter: day, week, month, year, or empty for no filter")]
            [Enum(["", "day", "week", "month", "year"])] string timeRange = "",
            [Description("Safe search level: none, moderate, strict")]
            [Enum(["none", "moderate", "strict"])] string safeSearch = "none",
            [Description("Maximum number of results to return"), Range(1, 50)] int maxResults = 10)
        {
            try
            {
                var searchQuery = BuildSearchQuery(query, page, language, timeRange, safeSearch, SearchCategory.Web, maxResults);
                var resultList = await _searchManager.SearchAllAsync(searchQuery);

                if (resultList.Results.Count == 0 && resultList.UnresponsiveEngines.Count > 0)
                {
                    var errors = string.Join("; ", resultList.UnresponsiveEngines.Select(e =>
                        $"{e.Engine}: {e.ErrorType}{(e.IsSuspended ? " (suspended)" : "")}"));
                    return new ToolResult(ToolResultStatus.Error, $"All search engines failed: {errors}");
                }

                var sb = new StringBuilder();
                var engineInfo = resultList.EngineTimings.Count > 0
                    ? string.Join(", ", resultList.EngineTimings.Select(t => $"{t.Engine} ({t.TotalTime}ms)"))
                    : "N/A";

                sb.AppendLine($"**Search results for \"{query}\"** (engines: {engineInfo})");
                sb.AppendLine();

                var count = 0;
                foreach (var result in resultList.Results.Take(maxResults))
                {
                    count++;
                    sb.AppendLine($"**{count}. [{result.Title}]({result.Url})**");
                    if (!string.IsNullOrEmpty(result.Source))
                        sb.AppendLine($"   *Source: {result.Source}*");
                    if (!string.IsNullOrEmpty(result.Content))
                        sb.AppendLine($"   {result.Content.Truncate(300)}");
                    if (result.PublishedDate.HasValue)
                        sb.AppendLine($"   *Published: {result.PublishedDate.Value:yyyy-MM-dd}*");
                    sb.AppendLine();
                }

                if (resultList.Suggestions.Count > 0)
                {
                    sb.AppendLine("**Suggestions:** " + string.Join(", ", resultList.Suggestions.Take(5)));
                    sb.AppendLine();
                }

                if (resultList.UnresponsiveEngines.Count > 0)
                {
                    sb.AppendLine($"*Note: {resultList.UnresponsiveEngines.Count} engine(s) did not respond*");
                }

                return new ToolResult(sb.ToString().Trim());
            }
            catch (Exception ex)
            {
                return new ToolResult(ToolResultStatus.Error, $"Web search failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Performs an image search across all available search engines.
        /// </summary>
        public async Task<ToolResult> SearchImagesAsync(
            [Description("The query to search by")] string query,
            [Description("The page number to return results for (1-based)"), Range(1, 10)] int page = 1,
            [Description("Language code (auto, en, ru, etc.)")] string language = "auto",
            [Description("Safe search level: none, moderate, strict")]
            [Enum(["none", "moderate", "strict"])] string safeSearch = "none",
            [Description("Maximum number of results to return"), Range(1, 50)] int maxResults = 10)
        {
            try
            {
                var searchQuery = BuildSearchQuery(query, page, language, "", safeSearch, SearchCategory.Images, maxResults);
                var resultList = await _searchManager.SearchAllAsync(searchQuery);

                var sb = new StringBuilder();
                sb.AppendLine($"**Image search results for \"{query}\"**");
                sb.AppendLine();

                var count = 0;
                foreach (var result in resultList.Results.Take(maxResults))
                {
                    count++;
                    sb.AppendLine($"**{count}. [{result.Title}]({result.Url})**");
                    if (!string.IsNullOrEmpty(result.ImgSrc))
                        sb.AppendLine($"   ![Image]({result.ImgSrc})");
                    if (!string.IsNullOrEmpty(result.Resolution))
                        sb.AppendLine($"   *Resolution: {result.Resolution}*");
                    if (!string.IsNullOrEmpty(result.Source))
                        sb.AppendLine($"   *Source: {result.Source}*");
                    if (!string.IsNullOrEmpty(result.Content))
                        sb.AppendLine($"   {result.Content.Truncate(200)}");
                    sb.AppendLine();
                }

                return new ToolResult(sb.ToString().Trim());
            }
            catch (Exception ex)
            {
                return new ToolResult(ToolResultStatus.Error, $"Image search failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Performs a news search across all available search engines.
        /// </summary>
        public async Task<ToolResult> SearchNewsAsync(
            [Description("The query to search by")] string query,
            [Description("The page number to return results for (1-based)"), Range(1, 10)] int page = 1,
            [Description("Language code (auto, en, ru, etc.)")] string language = "auto",
            [Description("Time range filter: day, week, month, year, or empty for no filter")]
            [Enum(["", "day", "week", "month", "year"])] string timeRange = "week",
            [Description("Maximum number of results to return"), Range(1, 50)] int maxResults = 10)
        {
            try
            {
                var searchQuery = BuildSearchQuery(query, page, language, timeRange, SafeSearchLevel.Moderate, SearchCategory.News, maxResults);
                var resultList = await _searchManager.SearchAllAsync(searchQuery);

                var sb = new StringBuilder();
                sb.AppendLine($"**News search results for \"{query}\"**");
                sb.AppendLine();

                var count = 0;
                foreach (var result in resultList.Results.Take(maxResults))
                {
                    count++;
                    sb.AppendLine($"**{count}. [{result.Title}]({result.Url})**");
                    if (!string.IsNullOrEmpty(result.Source) || !string.IsNullOrEmpty(result.Metadata))
                        sb.AppendLine($"   *{result.Metadata ?? result.Source}*");
                    if (result.PublishedDate.HasValue)
                        sb.AppendLine($"   *{result.PublishedDate.Value:yyyy-MM-dd}*");
                    if (!string.IsNullOrEmpty(result.Content))
                        sb.AppendLine($"   {result.Content.Truncate(250)}");
                    sb.AppendLine();
                }

                return new ToolResult(sb.ToString().Trim());
            }
            catch (Exception ex)
            {
                return new ToolResult(ToolResultStatus.Error, $"News search failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Builds a <see cref="SearchQuery"/> from tool parameters.
        /// </summary>
        private static SearchQuery BuildSearchQuery(
            string query, int page, string language, string timeRange, string safeSearch,
            SearchCategory category, int maxResults)
        {
            var safeSearchLevel = safeSearch switch
            {
                "none" => SafeSearchLevel.None,
                "moderate" => SafeSearchLevel.Moderate,
                "strict" => SafeSearchLevel.Strict,
                _ => SafeSearchLevel.None,
            };

            return BuildSearchQuery(query, page, language, timeRange, safeSearchLevel, category, maxResults);
        }

        /// <summary>
        /// Builds a <see cref="SearchQuery"/> from typed parameters.
        /// </summary>
        private static SearchQuery BuildSearchQuery(
            string query, int page, string language, string timeRange,
            SafeSearchLevel safeSearch, SearchCategory category, int maxResults)
        {
            TimeRange? parsedTimeRange = timeRange?.ToLowerInvariant() switch
            {
                "day" => TimeRange.Day,
                "week" => TimeRange.Week,
                "month" => TimeRange.Month,
                "year" => TimeRange.Year,
                _ => null,
            };

            return new SearchQuery
            {
                Query = query,
                Page = page,
                Category = category,
                Language = string.IsNullOrEmpty(language) || language == "auto" ? null : language,
                TimeRange = parsedTimeRange,
                SafeSearch = safeSearch,
                MaxResults = maxResults,
                TimeoutSeconds = 15.0,
            };
        }
    }

    /// <summary>
    /// Extension methods for string truncation.
    /// </summary>
    internal static class StringExtensions
    {
        /// <summary>
        /// Truncates a string to the specified maximum length, appending "..." if truncated.
        /// </summary>
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value[..maxLength] + "...";
        }
    }
}
