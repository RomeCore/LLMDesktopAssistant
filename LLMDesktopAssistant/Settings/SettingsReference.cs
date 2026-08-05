using System.Text.Json.Serialization;

namespace LLMDesktopAssistant.Settings
{
	/// <summary>
	/// Represents a lazy reference to a settings instance that is resolved through <see cref="SettingsManager"/>.
	/// </summary>
	/// <typeparam name="T">The settings type, must inherit from <see cref="SettingsObject"/> and have a parameterless constructor.</typeparam>
	/// <remarks>
	/// The reference is serialized as the ID of the referenced settings instance. The instance itself is
	/// resolved lazily on first access and cached.
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
		public T Object
		{
			get => _object ??= SettingsManager.Get<T>(Id);
			set
			{
				if (SetProperty(ref _object, value))
				{
					_id = value?.Id;
					RaisePropertyChanged(nameof(Id));
				}
			}
		}
	}
}
