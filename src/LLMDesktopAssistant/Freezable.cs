using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json.Serialization;
using LiteDB;
using YamlDotNet.Serialization;

namespace LLMDesktopAssistant
{
	/// <summary>
	/// A custom attribute to indicate that a property should not be frozen.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class NotFrozenAttribute : Attribute { }

	/// <summary>
	/// A base class for objects that can be frozen to prevent further modifications.
	/// </summary>
	public class Freezable : NotifyPropertyChanged
	{
		private static readonly ConcurrentDictionary<Type, List<string>> _notFrozenPropertiesCache = [];

		private bool _isFrozen = false;
		private HashSet<string>? _notFrozenProperties;

		/// <summary>
		/// Gets a value indicating whether the object is frozen.
		/// </summary>
		[JsonIgnore]
		[BsonIgnore]
		[YamlIgnore]
		public bool IsFrozen => _isFrozen;

		/// <summary>
		/// Gets a list of properties that should not be frozen.
		/// </summary>
		protected virtual IEnumerable<string> NotFrozenProperties => [];

		/// <summary>
		/// Freezes the object, preventing further modifications.
		/// </summary>
		public void Freeze()
		{
			if (!_isFrozen)
			{
				_isFrozen = true;

				_notFrozenProperties = _notFrozenPropertiesCache.GetOrAdd(GetType(), type =>
				{
					var result = new List<string>();
					var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
					foreach (var property in properties)
					{
						if (property.IsDefined(typeof(NotFrozenAttribute), true))
							result.Add(property.Name);
					}
					return result;
				}).Concat(NotFrozenProperties).ToHashSet();
			}
		}

		protected override void OnPropertyChanging(string? propertyName, object? oldValue = null, object? newValue = null)
		{
			if (_isFrozen && (string.IsNullOrEmpty(propertyName) || !_notFrozenProperties!.Contains(propertyName)))
				throw new InvalidOperationException("Cannot change properties of a frozen object.");
			base.OnPropertyChanging(propertyName, oldValue, newValue);
		}
	}
}