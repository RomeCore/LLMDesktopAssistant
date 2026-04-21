using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json.Serialization;
using LiteDB;
using RCParsing.Utils;

namespace LLMDesktopAssistant
{
	/// <summary>
	/// Represents data associated with an event.
	/// </summary>
	public abstract class EventData : Disposable
	{
		/// <summary>
		/// Populates the event data with the provided handler object.
		/// </summary>
		/// <param name="handlerObject">The object that contains handler methods to subscribe to the event.</param>
		public abstract void PopulateWith(object handlerObject);

		/// <summary>
		/// Populates the event data with the provided handler object.
		/// </summary>
		/// <param name="handlerObject">The object that contains handler methods to subscribe to the event.</param>
		/// <param name="unsubscriber">The disposable object that can be used to unsubscribe from the event.</param>
		public abstract void PopulateWith(object handlerObject, out IDisposable unsubscriber);
	}

	/// <summary>
	/// Represents data associated with an event.
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="THandler"></typeparam>
	public abstract class EventData<TKey, THandler> : EventData
		where THandler : Delegate
	{
		private static readonly ConcurrentDictionary<(Type HandlerType, Type EventDataType),
			MultiValueDictionary<TKey, THandler>> _handlersCache = [];

		private MultiValueDictionary<TKey, THandler> BuildHandlers((Type HandlerType, Type EventDataType) t)
		{
			var handlerType = t.HandlerType;
			var baseHandlers = GetHandlers(handlerType.BaseType);
			var handlers = new MultiValueDictionary<TKey, THandler>();

			foreach (var baseHandler in baseHandlers)
				handlers.Add(baseHandler.Key, baseHandler.Value.ToList());

			var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			var methods = handlerType.GetMethods(flags);

			foreach (var method in methods)
			{
				try
				{
					var handlerPair = TryCreateHandlerDelegate(method);
					if (handlerPair == null)
						continue;

					var (keys, handler) = handlerPair.Value;
					foreach (var key in keys)
						handlers.Add(key, handler);
				}
				catch (Exception ex)
				{
					throw new InvalidOperationException($"Failed to create handler for method {method.Name} in type {handlerType}", ex);
				}
			}

			return handlers;
		}

		private MultiValueDictionary<TKey, THandler> GetHandlers(Type? handlerType)
		{
			if (handlerType == null)
				return [];

			return _handlersCache.GetOrAdd((handlerType, GetType()), BuildHandlers);
		}

		/// <summary>
		/// The event records that store the event handlers and their associated objects.
		/// </summary>
		/// <typeparam name="THandler">The type of the event handler.</typeparam>
		/// <param name="MethodTarget">The object that holds the method that handler created from.</param>
		/// <param name="Handler">The event handler.</param>
		private record EventRecord(object MethodTarget, THandler Handler);

		private readonly MultiValueDictionary<TKey, EventRecord> _eventsMap = [];
		private readonly List<EventRecord> _allEvents = [];
		private readonly object _lockObject = new();

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				lock (_lockObject)
				{
					_eventsMap.Clear();
					_allEvents.Clear();
				}
			}
		}

		public sealed override void PopulateWith(object handlerObject)
		{
			var rawHandlers = GetHandlers(handlerObject.GetType());
			var handlerMap = new Dictionary<THandler, EventRecord>();

			lock (_lockObject)
			{
				foreach (var (key, handlers) in rawHandlers)
				{
					foreach (var handler in handlers)
					{
						if (!handlerMap.TryGetValue(handler, out var record))
						{
							record = new EventRecord(handlerObject, handler);
							handlerMap.Add(handler, record);
							_allEvents.Add(record);
						}
						_eventsMap.Add(key, record);
					}
				}
			}
		}

		public sealed override void PopulateWith(object handlerObject, out IDisposable unsubscriber)
		{
			var rawHandlers = GetHandlers(handlerObject.GetType());
			var handlerMap = new Dictionary<THandler, EventRecord>();

			var removeSet = new List<EventRecord>();
			var removeMap = new List<KeyValuePair<TKey, EventRecord>>();
			unsubscriber = null!;

			lock (_lockObject)
			{
				foreach (var (key, handlers) in rawHandlers)
				{
					foreach (var handler in handlers)
					{
						if (!handlerMap.TryGetValue(handler, out var record))
						{
							record = new EventRecord(handlerObject, handler);
							handlerMap.Add(handler, record);
							removeSet.Add(record);
							_allEvents.Add(record);
						}
						removeMap.Add(new KeyValuePair<TKey, EventRecord>(key, record));
						_eventsMap.Add(key, record);
					}
				}
			}

			unsubscriber = new Disposable(() =>
			{
				lock (_lockObject)
				{
					foreach (var pair in removeMap)
						_eventsMap.Remove(pair.Key, pair.Value);
					foreach (var record in removeSet)
						_allEvents.Remove(record);
				}
			});
		}

		/// <summary>
		/// Expands the keys for a given event handler. The keys is expanded when creating event subscriptions via <see cref="Subscribe(TKey, object, THandler)"/> method.
		/// </summary>
		protected virtual IEnumerable<TKey> ExpandBuildKeys(TKey key)
		{
			return [key];
		}

		/// <summary>
		/// Expands the keys for a given event handler. The keys is expanded when calling event handlers via <see cref="Call(TKey, Action{object, THandler})"/> method.
		/// </summary>
		protected virtual IEnumerable<TKey> ExpandCallKeys(TKey key)
		{
			return [key];
		}

		/// <summary>
		/// Creates a handler delegate from a given method. Returns null if the method should be skipped (for example, if it does not have the required attributes).
		/// </summary>
		/// <param name="method">The method to create a handler delegate for.</param>
		/// <returns>A tuple containing the keys and handler delegate, or null if the method should be skipped.</returns>
		protected abstract (IEnumerable<TKey>, THandler)? TryCreateHandlerDelegate(MethodInfo method);

		/// <summary>
		/// Subscribes a handler to the event with the specified key.
		/// </summary>
		/// <param name="key">The key to subscribe the handler to.</param>
		/// <param name="handler">The handler to subscribe.</param>
		public void Subscribe(TKey key, object caller, THandler handler)
		{
			var expandedKeys = ExpandBuildKeys(key);
			var record = new EventRecord(caller, handler);

			lock (_lockObject)
			{
				foreach (var expandedKey in expandedKeys)
					_eventsMap.Add(expandedKey, record);
				_allEvents.Add(record);
			}
		}

		/// <summary>
		/// Subscribes a handler to the event with the specified key.
		/// </summary>
		/// <param name="key">The key to subscribe the handler to.</param>
		/// <param name="handler">The handler to subscribe.</param>
		/// <param name="unsubscriber">The disposable object that can be used to unsubscribe from the event.</param>
		public void Subscribe(TKey key, object caller, THandler handler, out IDisposable unsubscriber)
		{
			var expandedKeys = ExpandBuildKeys(key).ToList();
			var record = new EventRecord(caller, handler);

			lock (_lockObject)
			{
				foreach (var expandedKey in expandedKeys)
					_eventsMap.Add(expandedKey, record);
				_allEvents.Add(record);
			}

			unsubscriber = new Disposable(() =>
			{
				lock (_lockObject)
				{
					foreach (var expandedKey in expandedKeys)
						_eventsMap.Remove(expandedKey, record);
					_allEvents.Remove(record);
				}
			});
		}

		/// <summary>
		/// Calls the event handlers associated with a given key.
		/// </summary>
		/// <param name="key">The key to retrieve the event handlers for.</param>
		/// <param name="action">The action to invoke for each event handler. The first parameter is the caller object (that holds the event handler methods), and the second parameter is the handler delegate.</param>
		public void Call(TKey key, Action<object, THandler> action)
		{
			lock (_lockObject)
			{
				foreach (var subKey in ExpandCallKeys(key))
				{
					if (_eventsMap.TryGetValue(subKey, out var records))
						foreach (var record in records)
							action.Invoke(record.MethodTarget, record.Handler);
				}
			}
		}

		/// <summary>
		/// Calls all event handlers.
		/// </summary>
		/// <param name="action">The action to invoke for each event handler. The first parameter is the caller object (that holds the event handler methods), and the second parameter is the handler delegate.</param>
		public void CallAll(Action<object, THandler> action)
		{
			lock (_lockObject)
			{
				foreach (var record in _allEvents)
					action.Invoke(record.MethodTarget, record.Handler);
			}
		}
	}

	/// <summary>
	/// Represents an event object that can be used to manage, subscribe (via attributes and subscriber objects) and trigger events.
	/// </summary>
	public class EventObject : Disposable
	{
		/// <summary>
		/// Gets the value indicating whether to call <c>PopulateEventsWith(this)</c> inside constructor.
		/// </summary>
		[JsonIgnore]
		[BsonIgnore]
		protected virtual bool AutoSubscribeSelf => true;

		/// <summary>
		/// Initializes a new instance of the <see cref="EventObject"/> class.
		/// </summary>
		public EventObject()
		{
			if (AutoSubscribeSelf)
				SubscribeEventsWith(this);
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
			{
				foreach (var data in GetEventData())
					data.Dispose();
			}
		}

		/// <summary>
		/// Gets the all event data associated with this object. This method can be overridden to provide custom event data.
		/// </summary>
		/// <returns>The event data associated with this object.</returns>
		protected virtual IEnumerable<EventData> GetEventData() => [];

		/// <summary>
		/// Populates the subscriptions with the provided handler object. The handler object can contain methods that are decorated with certain attributes to indicate that they should be subscribed to events.
		/// </summary>
		/// <param name="handlerObject">The object that contains handler methods to subscribe to events.</param>
		public void SubscribeEventsWith(object handlerObject)
		{
			foreach (var eventData in GetEventData())
				eventData.PopulateWith(handlerObject);
		}

		/// <summary>
		/// Populates the subscriptions with the provided handler object. The handler object can contain methods that are decorated with certain attributes to indicate that they should be subscribed to events.
		/// </summary>
		/// <param name="handlerObject">The object that contains handler methods to subscribe to events.</param>
		/// <param name="unsubscriber">The disposable object that can be used to unsubscribe from events.</param>
		public void SubscribeEventsWith(object handlerObject, out IDisposable unsubscriber)
		{
			var unsubscribers = new List<IDisposable>();

			foreach (var eventData in GetEventData())
			{
				eventData.PopulateWith(handlerObject, out var unsubscriberItem);
				unsubscribers.Add(unsubscriberItem);
			}

			if (unsubscribers.Count == 0)
			{
				unsubscriber = Empty;
				return;
			}

			if (unsubscribers.Count == 1)
			{
				unsubscriber = unsubscribers[0];
				return;
			}

			unsubscriber = new Disposable(() =>
			{
				foreach (var disposable in unsubscribers)
					disposable.Dispose();
			});
		}
	}
}