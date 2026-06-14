using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using SearXSharp;
using SearXSharp.Models;

namespace LLMDesktopAssistant.Scripting.Lua
{
	/// <summary>
	/// Lua API for web search: <c>web.search()</c>.
	/// Registered in the global <c>web</c> namespace, not under <c>dass</c>.
	/// Returns structured result tables instead of formatted text.
	/// </summary>
	[LuaApi]
	public class LuaApiWebSearch : LuaApiBase
	{
		private readonly SearchEngineManager _searchManager;

		public override string? Namespace => "web";

		public override string? Manuals => """
			--- web — web search API

			Provides web search functionality returning structured results.
			All results are returned as tables (objects) with named fields.

			FUNCTIONS:

			--- web.search(query, [options])
			  Searches the web using all available search engines.

			  Parameters:
			    - query: string (required) — The search query
			    - options: table (optional) — Optional parameters:
			      - page: number (default: 1) — Page number (1-10)
			      - language: string (default: "auto") — Language code, "auto", "en", "ru", "fr" etc.
			      - timeRange: string (default: "") — "day", "week", "month", "year" or ""
			      - safeSearch: string (default: "none") — "none", "moderate", "strict"
			      - category: string (default: "web") — Search category. Available values:
			        "all" — all categories (General)
			        "web" — web pages
			        "images" — image search
			        "videos" — video search
			        "news" — news articles
			        "science" — scientific publications
			        "it" — information technology (repos, code)
			        "files" — file hosting & downloads
			        "socialmedia" — social media content
			        "music" — music & audio
			        "map" — maps & geographic content
			        "repos" — code repositories (GitHub, GitLab)
			        "packages" — software packages (npm, PyPI, Docker)
			      - engines: table (optional) — Array of engine names (e.g. {"google", "bing"})
			      - maxResults: number (default: 10) — Max results per engine (1-50)

			  Returns: table with:
			    - results: array of result tables (see below)
			    - suggestions: array of strings
			    - corrections: array of strings
			    - total_results: number or nil
			    - engine_timings: array of strings
			    - unresponsive_engines: array of strings
			    - supports_paging: boolean

			  Each result table contains all fields from SearchResult:

			  Core fields:
			    - url (string)
			    - title (string)
			    - content (string)
			    - engine (string)
			    - engines (table of strings)
			    - template (string)
			    - score (number)
			    - type (string: "Default", "Image", "Video", etc.)
			    - category (string: "Web", "Images", "News", etc.)

			  Media fields:
			    - thumbnail (string or nil)
			    - img_src (string or nil)
			    - iframe_src (string or nil)
			    - audio_src (string or nil)

			  Time fields:
			    - published_date (string in ISO format or nil)
			    - duration (string "HH:MM:SS" or nil)

			  Metadata fields:
			    - author (string or nil)
			    - metadata (string or nil)
			    - source (string or nil)
			    - views (number or nil)
			    - resolution (string or nil)

			  Engine fields:
			    - engines (array of strings or nil)
			    - position (number)

			  Torrent fields:
			    - seed (number)
			    - leech (number)
			    - magnet_link (string or nil)

			  Geo fields:
			    - latitude (number)
			    - longitude (number)

			  Paper fields:
			    - doi (string or nil)
			    - authors (array of strings or nil)
			    - journal (string or nil)
			    - tags (array of strings or nil)
			    - comments (string or nil)
			    - pdf_url (string or nil)
			    - pages (string or nil)
			    - volume (string or nil)
			    - number (string or nil)

			EXAMPLES:

			  -- Simple search
			  local r = web.search("weather in London")
			  for _, res in ipairs(r.results) do
			    print(res.title .. " - " .. res.url)
			  end

			  -- Search with options
			  local r = web.search("Lua programming", {
			    engines = { "google", "bing" },
			    maxResults = 5,
			    category = "web"
			  })

			  -- Search images
			  local imgs = web.search("cats", {
			    category = "images",
			    maxResults = 10
			  })
			  for _, img in ipairs(imgs.results) do
			    print(img.title .. " [" .. (img.resolution or "?") .. "]")
			  end
			""";

		public LuaApiWebSearch(SearchEngineManager searchManager)
		{
			_searchManager = searchManager;
		}

		public override void Populate(Table globals, Table ns, LuaService luaService)
		{
			ns["search"] = DynValue.NewCallback(new CallbackFunction(Search));
		}

		private DynValue Search(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count == 0)
				throw new ScriptRuntimeException("web.search() requires at least a query string.");

			var query = args[0].CastToString();
			if (query == null)
				throw new ScriptRuntimeException("First argument must be a string (query).");

			// Defaults
			int page = 1;
			string language = "auto";
			string? timeRange = null;
			string safeSearch = "none";
			string category = "web";
			string[]? engines = null;
			int maxResults = 10;

			// Parse optional options table
			if (args.Count > 1 && args[1].Type == DataType.Table)
			{
				var opts = args[1].Table;

				if (opts.Get("page") is DynValue pageVal && pageVal.Type == DataType.Number)
					page = (int)pageVal.Number;

				if (opts.Get("language") is DynValue langVal && langVal.Type == DataType.String)
					language = langVal.String;

				if (opts.Get("timeRange") is DynValue trVal && trVal.Type == DataType.String)
					timeRange = trVal.String;

				if (opts.Get("safeSearch") is DynValue ssVal && ssVal.Type == DataType.String)
					safeSearch = ssVal.String;

				if (opts.Get("category") is DynValue catVal && catVal.Type == DataType.String)
					category = catVal.String;

				if (opts.Get("maxResults") is DynValue mrVal && mrVal.Type == DataType.Number)
					maxResults = (int)mrVal.Number;

				if (opts.Get("engines") is DynValue engVal && engVal.Type == DataType.Table)
				{
					var engList = new List<string>();
					foreach (var e in engVal.Table.Values)
					{
						if (e.Type == DataType.String)
							engList.Add(e.String);
					}
					if (engList.Count > 0)
						engines = engList.ToArray();
				}
			}

			// Build search query
			var searchQuery = BuildSearchQuery(query, page, language, timeRange, safeSearch, category, engines, maxResults);

			// Execute search
			SearchResultList resultList;
			try
			{
				resultList = _searchManager.SearchAllAsync(searchQuery, default).Result;
			}
			catch (Exception ex)
			{
				throw new ScriptRuntimeException($"Web search failed: {ex.Message}");
			}

			// Build result table
			var script = ctx.OwnerScript;
			var rootTable = new Table(script);

			// Results array
			var resultsArray = new Table(script);
			int idx = 1;
			foreach (var result in resultList.Results)
			{
				resultsArray[idx] = ResultToTable(script, result);
				idx++;
			}
			rootTable["results"] = resultsArray;

			// Suggestions
			rootTable["suggestions"] = StringArrayToTable(script, resultList.Suggestions);

			// Corrections
			rootTable["corrections"] = StringArrayToTable(script, resultList.Corrections);

			// Total results
			if (resultList.TotalResults.HasValue)
				rootTable["total_results"] = DynValue.NewNumber(resultList.TotalResults.Value);

			// Engine timings
			var timingsArray = new Table(script);
			for (int i = 0; i < resultList.EngineTimings.Count; i++)
			{
				var t = resultList.EngineTimings[i];
				timingsArray[i + 1] = DynValue.NewString($"{t.Engine} ({t.TotalTime}ms)");
			}
			rootTable["engine_timings"] = timingsArray;

			// Unresponsive engines
			var unresponsiveArray = new Table(script);
			for (int i = 0; i < resultList.UnresponsiveEngines.Count; i++)
			{
				var u = resultList.UnresponsiveEngines[i];
				unresponsiveArray[i + 1] = DynValue.NewString($"{u.Engine}: {u.ErrorType}{(u.IsSuspended ? " (suspended)" : "")}");
			}
			rootTable["unresponsive_engines"] = unresponsiveArray;

			rootTable["supports_paging"] = DynValue.NewBoolean(resultList.SupportsPaging);

			return DynValue.NewTable(rootTable);
		}

		private static Table ResultToTable(Script script, SearchResult r)
		{
			var t = new Table(script);

			// Core fields
			SetField(t, "url", r.Url);
			SetField(t, "title", r.Title);
			SetField(t, "content", r.Content);
			SetField(t, "engine", r.Engine);
			SetField(t, "template", r.Template);
			t["score"] = DynValue.NewNumber(r.Score);
			SetField(t, "type", r.Type.ToString());
			SetField(t, "category", r.Category.ToString());

			// Media fields
			SetField(t, "thumbnail", r.Thumbnail);
			SetField(t, "img_src", r.ImgSrc);
			SetField(t, "iframe_src", r.IframeSrc);
			SetField(t, "audio_src", r.AudioSrc);

			// Time fields
			if (r.PublishedDate.HasValue)
				t["published_date"] = DynValue.NewString(r.PublishedDate.Value.ToString("O"));
			if (r.Duration.HasValue && r.Duration.Value > TimeSpan.Zero)
				t["duration"] = DynValue.NewString(r.Duration.Value.ToString(@"hh\:mm\:ss"));

			// Metadata fields
			SetField(t, "author", r.Author);
			SetField(t, "metadata", r.Metadata);
			SetField(t, "source", r.Source);
			if (r.Views.HasValue)
				t["views"] = DynValue.NewNumber(r.Views.Value);
			SetField(t, "resolution", r.Resolution);

			// Engine-specific — always return an array, even if empty
			{
				var engArr = new Table(script);
				int i = 1;
				foreach (var e in r.Engines)
				{
					engArr[i] = DynValue.NewString(e);
					i++;
				}
				if (i == 1)
				{
					engArr[i] = DynValue.NewString(r.Engine);
				}
				t["engines"] = engArr;
			}
			t["position"] = DynValue.NewNumber(r.Position);

			// Torrent fields
			t["seed"] = DynValue.NewNumber(r.Seed);
			t["leech"] = DynValue.NewNumber(r.Leech);
			SetField(t, "magnet_link", r.MagnetLink);

			// Geo fields
			t["latitude"] = DynValue.NewNumber(r.Latitude);
			t["longitude"] = DynValue.NewNumber(r.Longitude);

			// Paper fields
			SetField(t, "doi", r.Doi);
			if (r.Authors is { Count: > 0 })
				t["authors"] = StringArrayToTable(script, r.Authors);
			SetField(t, "journal", r.Journal);
			if (r.Tags is { Count: > 0 })
				t["tags"] = StringArrayToTable(script, r.Tags);
			SetField(t, "comments", r.Comments);
			SetField(t, "pdf_url", r.PdfUrl);
			SetField(t, "pages", r.Pages);
			SetField(t, "volume", r.Volume);
			SetField(t, "number", r.Number);

			return t;
		}

		private static void SetField(Table t, string key, string? value)
		{
			if (value != null)
				t[key] = DynValue.NewString(value);
		}

		private static DynValue StringArrayToTable(Script script, IReadOnlyList<string>? list)
		{
			if (list == null || list.Count == 0)
				return DynValue.Nil;
			var arr = new Table(script);
			for (int i = 0; i < list.Count; i++)
				arr[i + 1] = DynValue.NewString(list[i]);
			return DynValue.NewTable(arr);
		}

		private static SearchQuery BuildSearchQuery(
			string query, int page, string language, string? timeRange, string safeSearch,
			string category, string[]? engines, int maxResults)
		{
			var searchCategory = category?.ToLowerInvariant() switch
			{
				"all" => SearchCategory.General,
				"web" => SearchCategory.Web,
				"images" => SearchCategory.Images,
				"videos" => SearchCategory.Videos,
				"news" => SearchCategory.News,
				"science" => SearchCategory.Science,
				"it" => SearchCategory.IT,
				"files" => SearchCategory.Files,
				"socialmedia" => SearchCategory.SocialMedia,
				"music" => SearchCategory.Music,
				"map" => SearchCategory.Map,
				"repos" => SearchCategory.Repos,
				"packages" => SearchCategory.Packages,
				_ => SearchCategory.General,
			};

			var safeSearchLevel = safeSearch switch
			{
				"none" => SafeSearchLevel.None,
				"moderate" => SafeSearchLevel.Moderate,
				"strict" => SafeSearchLevel.Strict,
				_ => SafeSearchLevel.None,
			};

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
				Category = searchCategory,
				Language = language,
				TimeRange = parsedTimeRange,
				SafeSearch = safeSearchLevel,
				Engines = engines,
				MaxResults = maxResults,
				TimeoutSeconds = 5.0,
			};
		}
	}
}
