using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using LLMDesktopAssistant.Utils;
using RCLargeLanguageModels.Tasks;

namespace LLMDesktopAssistant.Settings
{
	/// <summary>
	/// Manages a collection of settings objects of a specific type, handling persistence and change tracking.
	/// </summary>
	/// <typeparam name="TSettings">The settings type, must inherit from <see cref="SettingsObject"/> and have a parameterless constructor.</typeparam>
	public class SettingsCategory<TSettings>
		where TSettings : SettingsObject, new()
	{
		/// <summary>
		/// The debounce delay in milliseconds before saving changes to disk.
		/// </summary>
		public const int SaveDebounceDelayMs = 500;

		/// <summary>
		/// The default ID used for settings instances without an explicit identifier.
		/// </summary>
		public const string DefaultId = "$default";

		private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
		{
			WriteIndented = true,
			ReferenceHandler = ReferenceHandler.Preserve,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			Converters = { new JsonStringEnumConverter() }
		};

		private readonly string _name;
		private readonly string _filePath;
		private readonly ConcurrentDictionary<string, (TSettings, ChangeTracker)> _objects = [];
		private readonly Debounce _saveDebounce = new(debouncePassedResult: true);

		/// <summary>
		/// Initializes a new settings category with the specified name and folder location.
		/// </summary>
		/// <param name="name">The category name, used as the filename (with .json extension).</param>
		/// <param name="folder">The directory path where settings files are stored.</param>
		/// <exception cref="ArgumentException">Thrown when the category name is null, empty, or whitespace.</exception>
		public SettingsCategory(string name, string folder)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentException("Settings category name cannot be empty or contain only whitespace.", nameof(name));
			
			_name = name.Trim();
			_filePath = Path.Combine(folder, _name + ".json");

			Load();
		}

		private void Load()
		{
			if (!File.Exists(_filePath))
				return;

			var json = File.ReadAllText(_filePath);
			var data = JsonSerializer.Deserialize<Dictionary<string, TSettings>>(json, _jsonOptions);

			Unload();

			foreach (var (id, obj) in data ?? [])
			{
				var tracker = new ChangeTracker(obj, SaveDebounced);
				_objects.TryAdd(id, (obj, tracker));
			}
		}

		private void Unload()
		{
			foreach (var (obj, tracker) in _objects.Values)
			{
				tracker.Dispose();
				obj.Dispose();
			}

			_objects.Clear();
		}

		private async void SaveDebounced()
		{
			if (await _saveDebounce.DebounceAsync(millisecondsDelay: SaveDebounceDelayMs))
				Save();
		}

		/// <summary>
		/// Saves all settings in this category to disk immediately.
		/// </summary>
		/// <remarks>
		/// This method serializes the current state of all settings objects to JSON
		/// and writes them to the category's file.
		/// </remarks>
		public void Save()
		{
			var data = _objects.ToDictionary(k => k.Key, v => v.Value.Item1);
			var json = JsonSerializer.Serialize(data, _jsonOptions);
			File.WriteAllText(_filePath, json);
		}

		/// <summary>
		/// Retrieves a settings instance by ID, creating a new one if it doesn't exist.
		/// </summary>
		/// <param name="id">The unique identifier for the settings instance. Defaults to "$default".</param>
		/// <returns>The settings instance for the specified ID.</returns>
		/// <exception cref="ArgumentException">Thrown when the ID is null or whitespace.</exception>
		/// <remarks>
		/// Settings instances are cached and reused. Subsequent calls with the same ID return the cached instance.
		/// Changes to the returned object trigger automatic saving after a debounce delay.
		/// </remarks>
		public TSettings Get(string id = DefaultId)
		{
			if (string.IsNullOrWhiteSpace(id))
				throw new ArgumentException("Id cannot be null or whitespace.", nameof(id));
			return _objects.GetOrAdd(id, id =>
			{
				var obj = new TSettings();
				var tracker = new ChangeTracker(obj, SaveDebounced);
				return (obj, tracker);
			}).Item1;
		}

		/// <summary>
		/// Removes a settings instance by ID and disposes it.
		/// </summary>
		/// <param name="id">The unique identifier for the settings instance to remove.</param>
		/// <returns>true if the settings instance was found and removed; otherwise, false.</returns>
		/// <exception cref="ArgumentException">Thrown when the ID is null or whitespace.</exception>
		public bool Remove(string id)
		{
			if (string.IsNullOrWhiteSpace(id))
				throw new ArgumentException("Id cannot be null or whitespace.", nameof(id));
			if (_objects.TryRemove(id, out var objTuple))
			{
				objTuple.Item2.Dispose();
				objTuple.Item1.Dispose();
				return true;
			}
			return false;
		}

		/// <summary>
		/// Retrieves all available setting IDs currently loaded in memory.
		/// </summary>
		/// <returns>An array of setting IDs.</returns>
		public string[] GetAvailableIds()
		{
			return _objects.Keys.ToArray();
		}
	}
}