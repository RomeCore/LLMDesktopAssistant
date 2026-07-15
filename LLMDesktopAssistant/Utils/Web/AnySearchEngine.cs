using System.Net.Http.Json;
using System.Text.Json.Serialization;
using SearXSharp;
using SearXSharp.Models;

namespace LLMDesktopAssistant.Utils.Web;

/// <summary>
/// Search engine implementation for the AnySearch API.
/// Provides up to 1000 free queries per day for personal use.
/// API documentation: https://anysearch.com/docs
/// </summary>
public sealed class AnySearchEngine : SearchEngineBase
{
	private readonly string _apiEndpoint;
	private readonly string? _apiKey;

	/// <inheritdoc />
	public override string Name => "anysearch";

	/// <inheritdoc />
	public override string DisplayName => "AnySearch";

	/// <inheritdoc />
	public override IReadOnlyList<SearchCategory> SupportedCategories { get; }
		= [SearchCategory.General, SearchCategory.Web];

	/// <inheritdoc />
	public override bool SupportsPaging => false;

	/// <inheritdoc />
	public override bool SupportsTimeRange => false;

	/// <inheritdoc />
	public override bool SupportsSafeSearch => false;

	/// <inheritdoc />
	public override int MaxPages => 1;

	/// <inheritdoc />
	public override double Timeout => 15.0;

	/// <summary>
	/// Initializes a new instance with specified logger.
	/// </summary>
	public AnySearchEngine(ILogger logger) : base(logger)
	{
		_apiEndpoint = "https://api.anysearch.com/v1/search";
		_apiKey = null;
	}

	/// <summary>
	/// Initializes a new instance with default settings.
	/// </summary>
	public AnySearchEngine(string? apiKey, ILogger? logger = null) : base(logger ?? EmptyLogger.Instance)
	{
		_apiEndpoint = "https://api.anysearch.com/v1/search";
		_apiKey = apiKey;
	}

	/// <summary>
	/// Initializes a new instance with custom endpoint and API key.
	/// </summary>
	public AnySearchEngine(string apiEndpoint, string? apiKey, ILogger? logger = null) : base(logger ?? EmptyLogger.Instance)
	{
		_apiEndpoint = apiEndpoint;
		_apiKey = apiKey;
	}

	/// <inheritdoc />
	public override async Task<SearchResultList> SearchAsync(SearchQuery query, CancellationToken ct = default)
	{
		var validationError = ValidateQuery(query);
		if (validationError != null)
			return CreateErrorResult(validationError);
		
		if (string.IsNullOrEmpty(_apiKey))
			return CreateErrorResult("API key is required for this search engine.");

		try
		{
			var requestBody = new AnySearchRequest
			{
				Query = query.Query,
				MaxResults = Math.Min(query.MaxResults, 50)
			};

			var request = CreateJsonPostRequest(_apiEndpoint,
				System.Text.Json.JsonSerializer.Serialize(requestBody, AnySearchJsonContext.Default.AnySearchRequest));

			request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_apiKey}");

			var response = await SendRequestAsync(request, ct);
			response.EnsureSuccessStatusCode();

			var json = await response.Content.ReadAsStringAsync(ct);
			return ParseResponse(json);
		}
		catch (TaskCanceledException)
		{
			return CreateErrorResult("timeout", suspended: true);
		}
		catch (HttpRequestException ex)
		{
			_logger.Error(ex, "{Engine}: HTTP request failed for query: {Query}", Name, query.Query);
			return CreateErrorResult(ex.GetType().Name);
		}
		catch (Exception ex)
		{
			_logger.Error(ex, "{Engine}: Search failed for query: {Query}", Name, query.Query);
			return CreateErrorResult(ex.GetType().Name);
		}
	}

	private SearchResultList ParseResponse(string json)
	{
		var results = new List<SearchResult>();

		try
		{
			var response = System.Text.Json.JsonSerializer.Deserialize(json, AnySearchJsonContext.Default.AnySearchResponse);
			if (response?.Code != 0 || response.Data?.Results == null)
			{
				_logger.Warning("{Engine}: API returned code {Code}: {Message}", Name, response?.Code, response?.Message);
				return CreateResultList(results);
			}

			int position = 0;
			foreach (var item in response.Data.Results)
			{
				position++;
				results.Add(new SearchResult
				{
					Url = item.Url ?? string.Empty,
					Title = item.Title ?? string.Empty,
					Content = item.Content ?? item.Description ?? string.Empty,
					Engine = Name,
					Position = position,
					Category = SearchCategory.Web,
					Source = "AnySearch"
				});
			}
		}
		catch (System.Text.Json.JsonException ex)
		{
			_logger.Error(ex, "{Engine}: Failed to parse JSON response", Name);
		}

		return CreateResultList(results);
	}
}

/// <summary>
/// Request model for the AnySearch API.
/// </summary>
internal sealed class AnySearchRequest
{
	[JsonPropertyName("query")]
	public required string Query { get; init; }

	[JsonPropertyName("max_results")]
	public int MaxResults { get; init; } = 10;
}

/// <summary>
/// Response model for the AnySearch API.
/// </summary>
internal sealed class AnySearchResponse
{
	[JsonPropertyName("code")]
	public int Code { get; init; }

	[JsonPropertyName("message")]
	public string? Message { get; init; }

	[JsonPropertyName("data")]
	public AnySearchData? Data { get; init; }
}

internal sealed class AnySearchData
{
	[JsonPropertyName("results")]
	public List<AnySearchResultItem>? Results { get; init; }
}

internal sealed class AnySearchResultItem
{
	[JsonPropertyName("title")]
	public string? Title { get; init; }

	[JsonPropertyName("url")]
	public string? Url { get; init; }

	[JsonPropertyName("description")]
	public string? Description { get; init; }

	[JsonPropertyName("content")]
	public string? Content { get; init; }
}

/// <summary>
/// Source-generated JSON serialization context for AnySearch models.
/// </summary>
[JsonSerializable(typeof(AnySearchRequest))]
[JsonSerializable(typeof(AnySearchResponse))]
internal sealed partial class AnySearchJsonContext : JsonSerializerContext;
