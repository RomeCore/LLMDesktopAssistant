using System.Text.Json.Serialization;

namespace LLMDesktopAssistant.Settings
{
	/// <summary>
	/// Represents a lazy reference to a settings instance that is resolved through <see cref="SettingsManager"/>.
	/// </summary>
	/// <typeparam name="T">The settings type, must inherit from <see cref="SettingsObject"/> and have a parameterless constructor.</typeparam>
	/// <remarks>
	/// The reference is serialized as the ID of the referenced settings instance. The instance itself is
	/// resolved lazily on first access and cached. If the referenced instance does not exist,
	/// <see cref="Object"/> returns <see langword="null"/> and nothing is created.
	/// </remarks>
	public class SettingsReference<T> : NotifyPropertyChanged
		where T : SettingsObject, new()
	{
		private string? _id;
		private T? _object;

		/// <summary>
		/// Gets or sets the ID of the referenced settings instance.
		/// </summary>
		public string Id
		{
			get => _id ?? SettingsObject.DefaultId;
			set
			{
				if (SetProperty(ref _id, value))
					_object = null;
			}
		}

		/// <summary>
		/// Gets or sets the referenced settings instance, resolving it lazily through <see cref="SettingsManager"/> when not set explicitly.
		/// </summary>
		/// <remarks>
		/// Returns <see langword="null"/> when the referenced settings instance does not exist.
		/// </remarks>
		public T? Object
		{
			get
			{
				if (_object == null && SettingsManager.TryGet<T>(Id, out var obj))
					_object = obj;
				return _object;
			}
			set
			{
				if (SetProperty(ref _object, value))
				{
					var newId = value?.Id ?? _id;
					if (_id != newId)
					{
						_id = newId;
						RaisePropertyChanged(nameof(Id));
					}
				}
			}
		}
	}
}
