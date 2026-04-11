using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;

namespace LLMDesktopAssistant.Core.Utils
{
	/// <summary>
	/// Tracks changes to an object and its nested properties/collections by subscribing to
	/// <see cref="INotifyPropertyChanged"/> and <see cref="INotifyCollectionChanged"/> events.
	/// </summary>
	/// <remarks>
	/// Automatically tracks:
	/// - Property changes through INotifyPropertyChanged <br/>
	/// - Collection changes through INotifyCollectionChanged <br/>
	/// - Nested objects and collections <para/>
	/// 
	/// The tracker maintains a deep graph of tracked objects and cleans up event handlers
	/// when disposed.
	/// </remarks>
	public class ChangeTracker : IDisposable
	{
		private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = [];

		private readonly object _lock = new();
		private readonly object _root;
		private readonly Action _onChanged;
		private readonly HashSet<object> _trackedObjects;
		private readonly Dictionary<object, Dictionary<string, object?>> _propertyValues;
		private readonly Dictionary<object, object?[]> _collectionValues;

		/// <summary>
		/// Initializes a new instance of <see cref="ChangeTracker"/> class.
		/// </summary>
		/// <param name="root">The object to track recursively.</param>
		/// <param name="onChanged">The callback to call when object is changed.</param>
		/// <exception cref="ArgumentNullException">Thrown if any parameter is <see langword="null"/>.</exception>
		public ChangeTracker(object root, Action onChanged)
		{
			_root = root ?? throw new ArgumentNullException(nameof(root));
			_onChanged = onChanged ?? throw new ArgumentNullException(nameof(onChanged));
			_trackedObjects = new HashSet<object>(ReferenceEqualityComparer.Instance);
			_propertyValues = new Dictionary<object, Dictionary<string, object?>>(ReferenceEqualityComparer.Instance);
			_collectionValues = new Dictionary<object, object?[]>(ReferenceEqualityComparer.Instance);

			TrackObject(_root);
		}

		public void Dispose()
		{
			UntrackObject(_root);

			object?[] trackedArray;
			lock (_lock)
				trackedArray = _trackedObjects.ToArray();
			foreach (var tracked in trackedArray)
				UntrackObject(tracked);

			lock (_lock)
			{
				_trackedObjects.Clear();
				_propertyValues.Clear();
				_collectionValues.Clear();
			}
		}

		private void TrackObject(object? obj)
		{
			if (obj == null)
				return;

			var type = obj.GetType();
			if (IsIgnoredType(type))
				return;

			lock (_lock)
			{
				if (!_trackedObjects.Add(obj))
					return;
			}

			if (obj is IEnumerable en)
			{
				if (obj is not INotifyCollectionChanged incc)
					return;

				incc.CollectionChanged += OnCollectionChanged;

				var values = en.Cast<object?>().ToArray();

				lock (_lock)
					_collectionValues[obj] = values;

				foreach (var value in values)
					TrackObject(value);
			}
			else
			{
				if (obj is not INotifyPropertyChanged inpc)
					return;

				inpc.PropertyChanged += OnPropertyChanged;

				var properties = _propertyCache.GetOrAdd(type, static t => t
					.GetProperties(BindingFlags.Public | BindingFlags.Instance)
					.Where(p => p.CanRead && !p.GetIndexParameters().Any())
					.ToArray());

				foreach (var prop in properties)
				{
					var value = prop.GetValue(obj);
					UpdatePropertyValue(obj, prop.Name, value);
					TrackObject(value);
				}
			}
		}

		private void UntrackObject(object? obj)
		{
			if (obj == null)
				return;

			var type = obj.GetType();
			if (IsIgnoredType(type))
				return;

			lock (_lock)
			{
				if (!_trackedObjects.Remove(obj))
					return;
			}

			if (obj is IEnumerable en)
			{
				if (obj is not INotifyCollectionChanged incc)
					return;

				incc.CollectionChanged -= OnCollectionChanged;

				object?[] oldValues;
				lock (_lock)
					oldValues = _collectionValues[en];

				foreach (var item in oldValues)
					UntrackObject(item);
			}
			else
			{
				if (obj is not INotifyPropertyChanged inpc)
					return;

				inpc.PropertyChanged -= OnPropertyChanged;

				object?[]? properties = null;

				lock (_lock)
				{
					if (_propertyValues.TryGetValue(obj, out var props))
						properties = props.Select(p => p.Value).ToArray();
					_propertyValues.Remove(obj);
				}

				if (properties != null)
				{
					foreach (var prop in properties)
					{
						UntrackObject(prop);
					}
				}
			}
		}

		private void UpdatePropertyValue(object obj, string propertyName, object? value)
		{
			lock (_lock)
			{
				if (!_propertyValues.TryGetValue(obj, out var dict))
				{
					dict = new Dictionary<string, object?>();
					_propertyValues[obj] = dict;
				}
				dict[propertyName] = value;
			}
		}

		private bool TryGetPropertyValue(object obj, string propertyName, out object? value)
		{
			lock (_lock)
			{
				if (_propertyValues.TryGetValue(obj, out var dict) && dict.TryGetValue(propertyName, out value))
					return true;
			}
			value = null;
			return false;
		}

		private static bool IsIgnoredType(Type type)
		{
			return type.IsPrimitive || type.IsValueType || type == typeof(string);
		}

		private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			_onChanged();

			if (e.PropertyName == null)
				return;

			var obj = sender;
			var type = obj!.GetType();
			var property = type.GetProperty(e.PropertyName);
			if (property == null)
				return;

			TryGetPropertyValue(obj, e.PropertyName, out var oldValue);
			var newValue = property.GetValue(obj);

			if (!ReferenceEquals(oldValue, newValue))
			{
				UpdatePropertyValue(obj, e.PropertyName, newValue);

				if (oldValue != null)
					UntrackObject(oldValue);

				if (newValue != null)
					TrackObject(newValue);
			}
		}

		private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			_onChanged();

			if (sender is not IEnumerable en)
				return;

			if (e.OldItems != null)
			{
				foreach (var item in e.OldItems)
					UntrackObject(item);
			}

			if (e.NewItems != null)
			{
				foreach (var item in e.NewItems)
					TrackObject(item);
			}

			var newValues = en.Cast<object?>().ToArray();

			if (e.Action == NotifyCollectionChangedAction.Reset)
			{
				object?[] oldValues;
				lock (_lock)
					oldValues = _collectionValues[en];

				foreach (var item in oldValues)
					UntrackObject(item);
				foreach (var item in newValues)
					TrackObject(item);
			}

			lock (_lock)
				_collectionValues[en] = newValues;
		}
	}
}