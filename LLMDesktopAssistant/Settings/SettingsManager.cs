using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using LLMDesktopAssistant.Utils;
using LLMDesktopAssistant.Utils.Json;

namespace LLMDesktopAssistant.Settings
{
	/// <summary>
	/// Provides centralized management for settings objects with automatic persistence and change tracking.
	/// Settings are stored in JSON files organized by category, with automatic debounced saving.
	/// </summary>
	public static class SettingsManager
	{
		internal static readonly JsonSerializerOptions jsonOptions;

		static SettingsManager()
		{
			jsonOptions = new()
			{
				WriteIndented = true,
				ReferenceHandler = ReferenceHandler.Preserve,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
				TypeInfoResolver = JsonTypeInfoResolverCreator.Create(),
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
				Converters =
				{
					new JsonStringEnumConverter(),
					new SettingsReferenceConverter(),
				},
			};

			jsonOptions.MakeReadOnly();
		}

		private static readonly ConcurrentDictionary<Type, object> _categories = [];

		private static string GetCategoryName(Type type)
		{
			if (!typeof(SettingsObject).IsAssignableFrom(type))
				throw new ArgumentException($"Type must inherit from {nameof(SettingsObject)}", nameof(type));

			var attr = type.GetCustomAttribute<SettingsObjectAttribute>();
			return attr != null ? attr.Id : type.Name.ToLowerInvariant();
		}

		/// <summary>
		/// Gets or creates a settings category for the specified settings type.
		/// Categories are cached and reused across calls.
		/// </summary>
		/// <typeparam name="TSettings">The settings type, must inherit from <see cref="SettingsObject"/> and have a parameterless constructor.</typeparam>
		/// <returns>The settings category instance for the specified type.</returns>
		/// <remarks>
		/// Category names are determined by the <see cref="SettingsObjectAttribute"/> if present,
		/// otherwise the type name in lowercase is used.
		/// </remarks>
		public static SettingsCategory<TSettings> GetCategory<TSettings>()
			where TSettings : SettingsObject, new()
		{
			return (SettingsCategory<TSettings>)_categories.GetOrAdd(typeof(TSettings), _ =>
			{
				var name = GetCategoryName(typeof(TSettings));
				return new SettingsCategory<TSettings>(name, Directories.Settings);
			});
		}

		/// <summary>
		/// Retrieves all available IDs for the specified settings type.
		/// </summary>
		/// <typeparam name="TSettings">The settings type, must inherit from <see cref="SettingsObject"/> and have a parameterless constructor.</typeparam>
		/// <returns>An enumerable collection of setting IDs currently loaded in memory.</returns>
		public static IEnumerable<string> GetAvailableIds<TSettings>()
			where TSettings : SettingsObject, new()
		{
			var category = GetCategory<TSettings>();
			return category.GetAvailableIds();
		}

		/// <summary>
		/// Retrieves a settings instance by ID, creating a new one if it doesn't exist.
		/// </summary>
		/// <typeparam name="TSettings">The settings type, must inherit from <see cref="SettingsObject"/> and have a parameterless constructor.</typeparam>
		/// <param name="id">The unique identifier for the settings instance. Defaults to <see cref="SettingsObject.DefaultId"/>.</param>
		/// <returns>The settings instance for the specified ID.</returns>
		/// <exception cref="ArgumentException">Thrown when the ID is null or whitespace.</exception>
		/// <remarks>
		/// Settings instances are cached and reused. Subsequent calls with the same ID return the cached instance.
		/// Changes to the returned object are automatically persisted to disk after a debounce delay.
		/// </remarks>
		public static TSettings Get<TSettings>(string id = SettingsObject.DefaultId)
			where TSettings : SettingsObject, new()
		{
			var category = GetCategory<TSettings>();
			return category.Get(id);
		}

		/// <summary>
		/// Attempts to retrieve a settings instance by ID without creating a new one.
		/// </summary>
		/// <typeparam name="TSettings">The settings type, must inherit from <see cref="SettingsObject"/> and have a parameterless constructor.</typeparam>
		/// <param name="id">The unique identifier for the settings instance.</param>
		/// <param name="settings">When this method returns <see langword="true"/>, contains the settings instance; otherwise, <see langword="null"/>.</param>
		/// <returns><see langword="true"/> if a settings instance with the specified ID exists; otherwise, <see langword="false"/>.</returns>
		/// <exception cref="ArgumentException">Thrown when the ID is null or whitespace.</exception>
		/// <remarks>
		/// Unlike <see cref="Get{TSettings}(string)"/>, this method does not create a new settings instance when the ID is not found.
		/// </remarks>
		public static bool TryGet<TSettings>(string id, out TSettings? settings)
			where TSettings : SettingsObject, new()
		{
			var category = GetCategory<TSettings>();
			return category.TryGet(id, out settings);
		}

		/// <summary>
		/// Attempts to retrieve the default settings instance without creating a new one.
		/// </summary>
		/// <typeparam name="TSettings">The settings type, must inherit from <see cref="SettingsObject"/> and have a parameterless constructor.</typeparam>
		/// <param name="settings">When this method returns <see langword="true"/>, contains the settings instance; otherwise, <see langword="null"/>.</param>
		/// <returns><see langword="true"/> if a default settings instance exists; otherwise, <see langword="false"/>.</returns>
		public static bool TryGet<TSettings>(out TSettings? settings)
			where TSettings : SettingsObject, new()
		{
			return TryGet(SettingsObject.DefaultId, out settings);
		}


		/// <summary>
		/// Removes a settings instance by ID and disposes it.
		/// </summary>
		/// <typeparam name="TSettings">The settings type, must inherit from <see cref="SettingsObject"/> and have a parameterless constructor.</typeparam>
		/// <param name="id">The unique identifier for the settings instance to remove. Defaults to <see cref="SettingsObject.DefaultId"/>.</param>
		/// <returns>true if the settings instance was found and removed; otherwise, false.</returns>
		/// <exception cref="ArgumentException">Thrown when the ID is null or whitespace.</exception>
		public static bool Remove<TSettings>(string id = SettingsObject.DefaultId)
			where TSettings : SettingsObject, new()
		{
			var category = GetCategory<TSettings>();
			return category.Remove(id);
		}
	}
}