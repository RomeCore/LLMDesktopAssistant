using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.Modules
{
	/// <summary>
	/// Represents a tracker for dynamic modules. This class is used to manage the lifecycle of dynamic modules.
	/// </summary>
	public abstract class DynamicModuleTracker
	{
		/// <summary>
		/// Gets the current module associated with this tracker.
		/// </summary>
		public abstract IDynamicModule NonGenericModule { get; }

		/// <summary>
		/// Gets a set of available IDs for this tracker. These IDs can be used to identify the module.
		/// </summary>
		public abstract ImmutableHashSet<string> AvailableIds { get; }

		/// <summary>
		/// Gets or sets the ID of the current module associated with this tracker. These IDs can be used to identify the module.
		/// </summary>
		public abstract string ModuleId { get; set; }

	}

	/// <summary>
	/// Represents a tracker for dynamic modules. This class is used to manage the lifecycle of dynamic modules.
	/// </summary>
	/// <typeparam name="T">The type of the dynamic module to track.</typeparam>
	public sealed class DynamicModuleTracker<T> : DynamicModuleTracker
		where T : IDynamicModule
	{
		private readonly ImmutableDictionary<string, Type> _registry;

		private T _module;
		private string _moduleId;

		public override IDynamicModule NonGenericModule => _module;
		public override ImmutableHashSet<string> AvailableIds { get; }
		public override string ModuleId
		{
			get => _moduleId;
			set => SetFromId(value);
		}

		/// <summary>
		/// Gets the current module associated with this tracker.
		/// </summary>
		public T Module => _module;

		/// <summary>
		/// Event that is raised when the module changes. The first parameter is the old value and the second parameter is the new value.
		/// </summary>
		public event Action<T?, T>? OnChanged;

		/// <summary>
		/// Initializes a new instance of the <see cref="DynamicModuleTracker{T}"/> class.
		/// </summary>
		/// <param name="registry">A dictionary that maps IDs to module types.</param>
		/// <exception cref="ArgumentException">The registry is null or empty.</exception>
		public DynamicModuleTracker(ImmutableDictionary<string, Type> registry)
		{
			ArgumentNullException.ThrowIfNull(registry);
			if (registry.IsEmpty)
				throw new ArgumentException("The registry cannot be empty.", nameof(registry));

			_registry = registry;
			AvailableIds = [.. _registry.Keys];

			_moduleId = registry.Keys.First();
			_module = (T)Activator.CreateInstance(_registry[_moduleId])!;
		}

		private void Set(T module, string moduleId)
		{
			var previousModule = _module;
			_moduleId = moduleId;
			_module = module;

			previousModule?.Shutdown();
			module.Initialize();

			OnChanged?.Invoke(previousModule, module);
		}

		/// <summary>
		/// Sets the module from an ID. If the ID is not found in the registry, does nothing.
		/// </summary>
		/// <param name="id">The ID of the module to set.</param>
		public void TrySetFromId(string id)
		{
			ArgumentNullException.ThrowIfNull(id);

			if (!_registry.TryGetValue(id, out var type))
				return;

			var newModule = (T)Activator.CreateInstance(type)!;
			Set(newModule, id);
		}

		/// <summary>
		/// Sets the module from an ID. If the ID is not found in the registry, throws an exception.
		/// </summary>
		/// <param name="id">The ID of the module to set.</param>
		/// <exception cref="ArgumentException">The module with the specified ID was not found in the registry.</exception>
		public void SetFromId(string id)
		{
			ArgumentNullException.ThrowIfNull(id);

			if (!_registry.TryGetValue(id, out var type))
				throw new ArgumentException($"Module not found. Id: {id}", nameof(id));

			var newModule = (T)Activator.CreateInstance(type)!;
			Set(newModule, id);
		}
	}
}