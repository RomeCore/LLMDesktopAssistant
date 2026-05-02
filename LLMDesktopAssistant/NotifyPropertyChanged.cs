using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LLMDesktopAssistant
{
	/// <summary>
	/// The attribute used to mark methods that should be called when a specific property changes, applicable to <see cref="NotifyPropertyChanged"/> and its subscribers.
	/// </summary>
	/// <param name="propertyNames">The name of the properties to subscribe this method to.</param>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public class OnPropertyChangedAttribute(params string[] propertyNames) : Attribute
	{
		/// <summary>
		/// Gets the name of the property to subscribe this method to.
		/// </summary>
		public string[] PropertyNames { get; } = propertyNames ?? throw new ArgumentNullException(nameof(propertyNames));
	}

	/// <summary>
	/// Represents an event data class for property change events.
	/// </summary>
	public sealed class PropertyChangedEventData : EventData<string, Action<object, object?, object?>>
	{
		protected override (IEnumerable<string>, Action<object, object?, object?>)?
			TryCreateHandlerDelegate(MethodInfo method)
		{
			var attribute = method.GetCustomAttribute<OnPropertyChangedAttribute>();
			if (attribute == null || attribute.PropertyNames.Length == 0)
				return null;

			var parameters = method.GetParameters();
			var target = Expression.Parameter(typeof(object));
			var oldP = Expression.Parameter(typeof(object));
			var newP = Expression.Parameter(typeof(object));

			Expression call;

			if (parameters.Length == 0)
				call = Expression.Call(
					Expression.Convert(target, method.DeclaringType!),
					method
				);
			else if (parameters.Length == 1)
				call = Expression.Call(
					Expression.Convert(target, method.DeclaringType!),
					method,
					Expression.Convert(newP, parameters[0].ParameterType)
				);
			else if (parameters.Length == 2)
				call = Expression.Call(
					Expression.Convert(target, method.DeclaringType!),
					method,
					Expression.Convert(oldP, parameters[0].ParameterType),
					Expression.Convert(newP, parameters[1].ParameterType)
				);
			else
				throw new InvalidMethodSignatureException(method, "Expected 0 to 2 parameters containing the old value (optional) and the new value (optional, higher priority) of the properties.");

			return (attribute.PropertyNames, Expression.Lambda<Action<object, object?, object?>>(
				call, target, oldP, newP
			).Compile());
		}
	}

	/// <summary>
	/// Provides a base class for implementing INotifyPropertyChanged.
	/// </summary>
	public class NotifyPropertyChanged : EventObject, INotifyPropertyChanged
	{
		private readonly PropertyChangedEventData _propertyChangedEvt = new();

		protected override IEnumerable<EventData> GetEventData() => [_propertyChangedEvt];

		private PropertyChangedEventHandler? _propertyChangedHandler = null;
		public event PropertyChangedEventHandler? PropertyChanged
		{
			add => _propertyChangedHandler += value;
			remove => _propertyChangedHandler -= value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="NotifyPropertyChanged"/> class.
		/// </summary>
		public NotifyPropertyChanged()
		{
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			_propertyChangedHandler = null;
		}

		/// <summary>
		/// Raises the PropertyChanged event for a specific property.
		/// </summary>
		/// <param name="propertyName">The name of the property that has changed.</param>
		/// <param name="oldValue">The old value of the property.</param>
		/// <param name="newValue">The new value of the property.</param>
		protected void RaisePropertyChanged(string? propertyName, object? oldValue = null, object? newValue = null)
		{
			OnPropertyChanged(propertyName, oldValue, newValue);
			if (propertyName != null)
				_propertyChangedEvt.Call(propertyName, (c, e) => e(c, oldValue, newValue));
			_propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		/// <summary>
		/// Sets the value of a property and raises the PropertyChanged event if the value has changed.
		/// </summary>
		/// <typeparam name="T">The type of the property.</typeparam>
		/// <param name="backingField">The backing field for the property.</param>
		/// <param name="value">The new value of the property.</param>
		/// <param name="propertyName">The name of the property.</param>
		/// <returns>true if the value was set; otherwise, false.</returns>
		protected bool SetProperty<T>(ref T backingField, T value, [CallerMemberName] string propertyName = "")
		{
			if (!Equals(backingField, value))
			{
				var oldValue = backingField;
				backingField = value;
				RaisePropertyChanged(propertyName, oldValue, value);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Sets the value of a property and raises the PropertyChanged event if the value has changed.
		/// </summary>
		/// <typeparam name="T">The type of the property.</typeparam>
		/// <param name="backingField">The backing field for the property.</param>
		/// <param name="value">The new value of the property.</param>
		/// <param name="oldValue">The old value of the property.</param>
		/// <param name="propertyName">The name of the property.</param>
		/// <returns>true if the value was set; otherwise, false.</returns>
		protected bool SetProperty<T>(ref T backingField, T value, out T oldValue, [CallerMemberName] string propertyName = "")
		{
			if (!Equals(backingField, value))
			{
				oldValue = backingField;
				backingField = value;
				RaisePropertyChanged(propertyName, oldValue, value);
				return true;
			}
			oldValue = value;
			return false;
		}

		/// <summary>
		/// The method that is called when a property changes.
		/// </summary>
		/// <param name="propertyName">The name of the property that has changed.</param>
		/// <param name="oldValue">The old value of the property.</param>
		/// <param name="newValue">The new value of the property.</param>
		protected virtual void OnPropertyChanged(string? propertyName, object? oldValue = null, object? newValue = null)
		{
		}

		private void Subscribe(string propertyName, Action<object, object?, object?> handler, out IDisposable unsubscriber)
		{
			ArgumentNullException.ThrowIfNull(propertyName);
			_propertyChangedEvt.Subscribe(propertyName, this, handler, out unsubscriber);
		}

		private void Subscribe(string propertyName, Action<object, object?, object?> handler)
		{
			ArgumentNullException.ThrowIfNull(propertyName);
			_propertyChangedEvt.Subscribe(propertyName, this, handler);
		}

		/// <summary>
		/// Subscribes to a property change event.
		/// </summary>
		/// <param name="propertyName">The name of the property to subscribe to.</param>
		/// <param name="handler">The event handler to subscribe.</param>
		/// <returns>A disposable object that can be disposed to unsubscribe from the event.</returns>
		/// <exception cref="ArgumentNullException">The propertyName or handler parameter is null.</exception>
		public void SubscribeChanged(string propertyName, Action<object?> handler)
		{
			void _handler(object i, object? oldValue, object? newValue) => handler(newValue);
			Subscribe(propertyName, _handler);
		}

		/// <summary>
		/// Subscribes to a property change event.
		/// </summary>
		/// <param name="propertyName">The name of the property to subscribe to.</param>
		/// <param name="handler">The event handler to subscribe.</param>
		/// <returns>A disposable object that can be disposed to unsubscribe from the event.</returns>
		/// <exception cref="ArgumentNullException">The propertyName or handler parameter is null.</exception>
		public void SubscribeChanged(string propertyName, Action<object?, object?> handler)
		{
			void _handler(object i, object? oldValue, object? newValue) => handler(oldValue, newValue);
			Subscribe(propertyName, _handler);
		}

		/// <summary>
		/// Subscribes to a property change event.
		/// </summary>
		/// <param name="propertyName">The name of the property to subscribe to.</param>
		/// <param name="handler">The event handler to subscribe.</param>
		/// <returns>A disposable object that can be disposed to unsubscribe from the event.</returns>
		/// <exception cref="ArgumentNullException">The propertyName or handler parameter is null.</exception>
		public void SubscribeChanged<T>(string propertyName, Action<T?> handler)
		{
			void _handler(object i, object? oldValue, object? newValue) => handler((T?)newValue);
			Subscribe(propertyName, _handler);
		}

		/// <summary>
		/// Subscribes to a property change event.
		/// </summary>
		/// <param name="propertyName">The name of the property to subscribe to.</param>
		/// <param name="handler">The event handler to subscribe.</param>
		/// <returns>A disposable object that can be disposed to unsubscribe from the event.</returns>
		/// <exception cref="ArgumentNullException">The propertyName or handler parameter is null.</exception>
		public void SubscribeChanged<T>(string propertyName, Action<T?, T?> handler)
		{
			void _handler(object i, object? oldValue, object? newValue) => handler((T?)oldValue, (T?)newValue);
			Subscribe(propertyName, _handler);
		}

		/// <summary>
		/// Subscribes to a property change event.
		/// </summary>
		/// <param name="propertyName">The name of the property to subscribe to.</param>
		/// <param name="handler">The event handler to subscribe.</param>
		/// <returns>A disposable object that can be disposed to unsubscribe from the event.</returns>
		/// <exception cref="ArgumentNullException">The propertyName or handler parameter is null.</exception>
		public void SubscribeChanged<T>(string propertyName, Action<object, T?, T?> handler)
		{
			void _handler(object i, object? oldValue, object? newValue) => handler(i, (T?)oldValue, (T?)newValue);
			Subscribe(propertyName, _handler);
		}

		/// <summary>
		/// Subscribes to a property change event.
		/// </summary>
		/// <param name="propertyName">The name of the property to subscribe to.</param>
		/// <param name="handler">The event handler to subscribe.</param>
		/// <returns>A disposable object that can be disposed to unsubscribe from the event.</returns>
		/// <exception cref="ArgumentNullException">The propertyName or handler parameter is null.</exception>
		public void SubscribeChanged(string propertyName, Action<object?> handler, out IDisposable unsubscriber)
		{
			void _handler(object i, object? oldValue, object? newValue) => handler(newValue);
			Subscribe(propertyName, _handler, out unsubscriber);
		}

		/// <summary>
		/// Subscribes to a property change event.
		/// </summary>
		/// <param name="propertyName">The name of the property to subscribe to.</param>
		/// <param name="handler">The event handler to subscribe.</param>
		/// <returns>A disposable object that can be disposed to unsubscribe from the event.</returns>
		/// <exception cref="ArgumentNullException">The propertyName or handler parameter is null.</exception>
		public void SubscribeChanged(string propertyName, Action<object?, object?> handler, out IDisposable unsubscriber)
		{
			void _handler(object i, object? oldValue, object? newValue) => handler(oldValue, newValue);
			Subscribe(propertyName, _handler, out unsubscriber);
		}

		/// <summary>
		/// Subscribes to a property change event.
		/// </summary>
		/// <param name="propertyName">The name of the property to subscribe to.</param>
		/// <param name="handler">The event handler to subscribe.</param>
		/// <returns>A disposable object that can be disposed to unsubscribe from the event.</returns>
		/// <exception cref="ArgumentNullException">The propertyName or handler parameter is null.</exception>
		public void SubscribeChanged<T>(string propertyName, Action<T?> handler, out IDisposable unsubscriber)
		{
			void _handler(object i, object? oldValue, object? newValue) => handler((T?)newValue);
			Subscribe(propertyName, _handler, out unsubscriber);
		}

		/// <summary>
		/// Subscribes to a property change event.
		/// </summary>
		/// <param name="propertyName">The name of the property to subscribe to.</param>
		/// <param name="handler">The event handler to subscribe.</param>
		/// <returns>A disposable object that can be disposed to unsubscribe from the event.</returns>
		/// <exception cref="ArgumentNullException">The propertyName or handler parameter is null.</exception>
		public void SubscribeChanged<T>(string propertyName, Action<T?, T?> handler, out IDisposable unsubscriber)
		{
			void _handler(object i, object? oldValue, object? newValue) => handler((T?)oldValue, (T?)newValue);
			Subscribe(propertyName, _handler, out unsubscriber);
		}

		/// <summary>
		/// Subscribes to a property change event.
		/// </summary>
		/// <param name="propertyName">The name of the property to subscribe to.</param>
		/// <param name="handler">The event handler to subscribe.</param>
		/// <returns>A disposable object that can be disposed to unsubscribe from the event.</returns>
		/// <exception cref="ArgumentNullException">The propertyName or handler parameter is null.</exception>
		public void SubscribeChanged<T>(string propertyName, Action<object, T?, T?> handler, out IDisposable unsubscriber)
		{
			void _handler(object i, object? oldValue, object? newValue) => handler(i, (T?)oldValue, (T?)newValue);
			Subscribe(propertyName, _handler, out unsubscriber);
		}
	}
}