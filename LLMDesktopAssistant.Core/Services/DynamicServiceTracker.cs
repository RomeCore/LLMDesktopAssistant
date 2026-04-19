using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace LLMDesktopAssistant.Services
{
	/// <summary>
	/// Represents a tracker for dynamic modules. This class is used to manage the lifecycle of dynamic modules.
	/// </summary>
	public abstract class DynamicServiceTracker
	{
		/// <summary>
		/// Gets the current module associated with this tracker.
		/// </summary>
		public abstract IDynamicService? NonGenericModule { get; }

		/// <summary>
		/// Gets a set of available IDs for this tracker. These IDs can be used to identify the module.
		/// </summary>
		public abstract ImmutableHashSet<string> AvailableIds { get; }

		/// <summary>
		/// Gets or sets the ID of the current module associated with this tracker. These IDs can be used to identify the module.
		/// </summary>
		public abstract string? ModuleId { get; set; }

		/// <summary>
		/// Initializes the current module. Can be called only once at <see cref="ServiceRegistry"/> initialization.
		/// </summary>
		public abstract void Initialize();
	}

	/// <summary>
	/// Represents a tracker for dynamic modules. This class is used to manage the lifecycle of dynamic modules.
	/// </summary>
	/// <typeparam name="T">The type of the dynamic module to track.</typeparam>
	public sealed class DynamicServiceTracker<T> : DynamicServiceTracker
		where T : IDynamicService
	{
		private readonly ImmutableDictionary<string, DynamicServiceTypeInfo> _registry;

		private T? _module;
		private string? _moduleId;

		public override IDynamicService? NonGenericModule => _module;
		public override ImmutableHashSet<string> AvailableIds { get; }
		public override string? ModuleId
		{
			get => _moduleId;
			set => SetFromId(value);
		}

		/// <summary>
		/// Gets the current module associated with this tracker.
		/// </summary>
		public T? Module => _module;

		/// <summary>
		/// Event that is raised when the module changes. The first parameter is the old value and the second parameter is the new value.
		/// </summary>
		public event Action<T?, T?>? OnChanged;

		/// <summary>
		/// Initializes a new instance of the <see cref="DynamicServiceTracker{T}"/> class.
		/// </summary>
		/// <param name="registry">A dictionary that maps IDs to module types.</param>
		/// <exception cref="ArgumentException">The registry is null or empty.</exception>
		public DynamicServiceTracker(IEnumerable<DynamicServiceTypeInfo> registry)
		{
			ArgumentNullException.ThrowIfNull(registry);

			_registry = registry.ToImmutableDictionary(k => k.Id, v => v);
			AvailableIds = [.. _registry.Keys];

			DynamicServiceTypeInfo? defaultModule = null;
			int? maxPriority = null;
			foreach (var typeInfo in _registry.Values)
			{
				if (typeInfo.DefaultPriority.HasValue)
				{
					if (maxPriority.HasValue)
					{
						if (maxPriority.Value < typeInfo.DefaultPriority.Value)
						{
							maxPriority = typeInfo.DefaultPriority;
							defaultModule = typeInfo;
						}
					}
					else
					{
						maxPriority = typeInfo.DefaultPriority;
						defaultModule = typeInfo;
					}
				}
			}

			if (defaultModule != null)
			{
				_moduleId = defaultModule.Id;
				_module = (T)Activator.CreateInstance(defaultModule.Type)!;
			}
		}

		private void Set(T? module, string? moduleId)
		{
			var previousModule = _module;
			_moduleId = moduleId;
			_module = module;

			try
			{
				previousModule?.Shutdown();
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed shutting down module '{module}': {errmsg}\n{st}", previousModule, ex.Message, ex.StackTrace);
			}

			try
			{
				module?.Initialize();
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed initializing module '{module}': {errmsg}\n{st}", module, ex.Message, ex.StackTrace);
				module = default;
			}

			if (!Equals(previousModule, module))
				OnChanged?.Invoke(previousModule, module);
		}

		private bool _initialized = false;

		public override void Initialize()
		{
			if (_initialized)
				throw new InvalidOperationException("Dynamic module tracker is already initialized!");

			try
			{
				_module?.Initialize();
			}
			catch (Exception ex)
			{
				_module = default;
				_moduleId = null;
				Log.Error(ex, "Failed initializing module '{module}': {errmsg}\n{st}", _module, ex.Message, ex.StackTrace);
			}
		}

		/// <summary>
		/// Sets the module from an ID. If the ID is not found in the registry, does nothing.
		/// </summary>
		/// <param name="id">The ID of the module to set.</param>
		public void TrySetFromId(string? id)
		{
			ArgumentNullException.ThrowIfNull(id);

			if (!_registry.TryGetValue(id, out var typeInfo))
				return;

			var newModule = (T)Activator.CreateInstance(typeInfo.Type)!;
			Set(newModule, id);
		}

		/// <summary>
		/// Sets the module from an ID. If the ID is not found in the registry, throws an exception.
		/// </summary>
		/// <param name="id">The ID of the module to set.</param>
		/// <exception cref="ArgumentException">The module with the specified ID was not found in the registry.</exception>
		public void SetFromId(string? id)
		{
			if (id == null)
			{
				Set(default, null);
				return;
			}

			if (!_registry.TryGetValue(id, out var typeInfo))
				throw new ArgumentException($"Module not found. Id: {id}", nameof(id));

			var newModule = (T)Activator.CreateInstance(typeInfo.Type)!;
			Set(newModule, id);
		}

		/// <summary>
		/// Gets the current module and ensures that it will not be <see langword="null"/>.
		/// </summary>
		/// <returns>Current module that is not <see langword="null"/>.</returns>
		/// <exception cref="ServiceRequiredException">Current module is <see langword="null"/>.</exception>
		public T Require()
		{
			if (_module is null)
				throw new ServiceRequiredException($"{typeof(T).FullName} is required for this operation");
			return _module;
		}
	}
}