using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace LLMDesktopAssistant.Utils
{
	/// <summary>
	/// Represents a constrained type.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public readonly struct ConstrainedType<T>
	{
		/// <summary>
		/// Gets the underlying type.
		/// </summary>
		public readonly Type Type;

		/// <summary>
		/// Initializes a new instance of the <see cref="ConstrainedType{T}"/> struct.
		/// </summary>
		/// <param name="type">The type.</param>
		public ConstrainedType(Type type)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));
			if (!typeof(T).IsAssignableFrom(type))
				throw new ArgumentException($"Type '{type}' is not assignable to '{typeof(T)}'", nameof(type));
			Type = type;
		}
	}

	/// <summary>
	/// Represents a pair of type and its attribute.
	/// </summary>
	/// <typeparam name="TAttribute">The type of attribute.</typeparam>
	public readonly struct TypeAttributePair<TAttribute> where TAttribute : Attribute
	{
		/// <summary>
		/// The type associated with the attribute.
		/// </summary>
		public readonly Type Type;

		/// <summary>
		/// The attribute associated with and applied to the type.
		/// </summary>
		public readonly TAttribute Attribute;

		/// <summary>
		/// Initializes a new instance of the <see cref="TypeAttributePair{TAttribute}"/> struct.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <param name="attribute">The attribute.</param>
		public TypeAttributePair(Type type, TAttribute attribute)
		{
			Type = type;
			Attribute = attribute;
		}
	}

	/// <summary>
	/// Represents a pair of type and its attribute.
	/// </summary>
	/// <typeparam name="T">The type.</typeparam>
	/// <typeparam name="TAttribute">The type of attribute.</typeparam>
	public readonly struct TypeAttributePair<T, TAttribute> where TAttribute : Attribute
	{
		/// <summary>
		/// The type associated with the attribute.
		/// </summary>
		public readonly Type Type;

		/// <summary>
		/// The attribute associated with and applied to the type.
		/// </summary>
		public readonly TAttribute Attribute;

		/// <summary>
		/// Initializes a new instance of the <see cref="TypeAttributePair{T, Attribute}"/> struct.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <param name="attribute">The attribute.</param>
		public TypeAttributePair(Type type, TAttribute attribute)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));
			if (!typeof(T).IsAssignableFrom(type))
				throw new ArgumentException($"Type '{type}' is not assignable to '{typeof(T)}'", nameof(type));
			Type = type;
			Attribute = attribute;
		}
	}

	/// <summary>
	/// Provides utility methods for reflection.
	/// </summary>
	public static class ReflectionUtility
	{
		/// <summary>
		/// Registers the assembly for <see cref="ReflectionUtility"/> observation.
		/// </summary>
		[AttributeUsage(AttributeTargets.Assembly)]
		public class RegisterAttribute : Attribute { }

		private static bool _initialized;
		private static Assembly[] _allAssemblies;
		private static Assembly[] _observedAssemblies;
		private static Type[] _allTypes;
		private static Type[] _observedTypes;

		/// <summary>
		/// Safely selects types from an assembly.
		/// </summary>
		private static IEnumerable<Type> GetTypesSafe(this IEnumerable<Assembly> assemblies, Func<Assembly, IEnumerable<Type>> selector)
		{
			foreach (var assembly in assemblies)
			{
				Type[] types;
				try
				{
					types = selector(assembly).ToArray();
				}
				catch (ReflectionTypeLoadException ex)
				{
					Log.Error(ex, "Failed to load types from assembly {AssemblyName}", assembly.FullName);
					types = ex.Types.Where(t => t != null).ToArray()!;
				}
				foreach (var type in types)
					yield return type;
			}
		}

		static ReflectionUtility()
		{
			_allAssemblies = null!;
			_observedAssemblies = null!;
			_allTypes = null!;
			_observedTypes = null!;
		}

		private static void CheckInitialized()
		{
			if (!_initialized)
				throw new InvalidOperationException("ReflectionUtility has not been initialized.");
		}

		/// <summary>
		/// Initializes the <see cref="ReflectionUtility"/> class.
		/// </summary>
		/// <param name="domain">The application domain to initialize with.</param>
		public static void Initialize(AppDomain domain)
		{
			if (_initialized)
				throw new InvalidOperationException("ReflectionUtility has already been initialized.");

			_allAssemblies = domain.GetAssemblies();
			_observedAssemblies = _allAssemblies
				.Where(a => a.GetCustomAttributes(typeof(RegisterAttribute), false).Length > 0)
				.ToArray();

			_allTypes = _allAssemblies
				.GetTypesSafe(a => a.GetTypes())
				.ToArray();

			_observedTypes = _observedAssemblies
				.GetTypesSafe(a => a.GetTypes())
				.ToArray();

			_initialized = true;
		}

		/// <summary>
		/// Gets all loaded assemblies.
		/// </summary>
		public static IReadOnlyList<Assembly> AllAssemblies
		{
			get
			{
				CheckInitialized();
				return _allAssemblies;
			}
		}

		/// <summary>
		/// Gets all assemblies registered with <see cref="RegisterAttribute"/>.
		/// </summary>
		public static IReadOnlyList<Assembly> ObservedAssemblies
		{
			get
			{
				CheckInitialized();
				return _observedAssemblies;
			}
		}

		/// <summary>
		/// Gets all types from all loaded assemblies.
		/// </summary>
		public static IReadOnlyList<Type> AllTypes
		{
			get
			{
				CheckInitialized();
				return _allTypes;
			}
		}

		/// <summary>
		/// Gets all types from observed assemblies (i.e., those with <see cref="RegisterAttribute"/> applied).
		/// </summary>
		public static IReadOnlyList<Type> ObservedTypes
		{
			get
			{
				CheckInitialized();
				return _observedTypes;
			}
		}

		/// <summary>
		/// Returns all types implementing specified interface or inheriting a given base type.
		/// </summary>
		public static IEnumerable<Type> GetTypes<TBase>(bool observedOnly = true)
		{
			CheckInitialized();
			var set = observedOnly ? _observedTypes : _allTypes;
			var baseType = typeof(TBase);
			return set.Where(t => baseType.IsAssignableFrom(t) && t != baseType);
		}

		/// <summary>
		/// Finds a type by its full name (optionally only among observed assemblies).
		/// </summary>
		public static Type? GetType(string fullName, bool observedOnly = true)
		{
			CheckInitialized();
			var set = observedOnly ? _observedTypes : _allTypes;
			return set.FirstOrDefault(t => t.FullName == fullName);
		}

		/// <summary>
		/// Gets all types that satisfy the given predicate.
		/// </summary>
		public static IEnumerable<Type> GetTypes(Func<Type, bool> predicate, bool observedOnly = true)
		{
			CheckInitialized();
			var set = observedOnly ? _observedTypes : _allTypes;
			return set.Where(predicate);
		}

		/// <summary>
		/// Gets all types marked with specified attribute.
		/// </summary>
		public static IEnumerable<TypeAttributePair<TAttribute>> GetTypesWithAttribute<TAttribute>(bool observedOnly = true)
			where TAttribute : Attribute
		{
			CheckInitialized();
			var set = observedOnly ? _observedTypes : _allTypes;
			return set
				.Select(t => new TypeAttributePair<TAttribute>(t, t.GetCustomAttribute<TAttribute>(inherit: true)!))
				.Where(t => t.Attribute != null);
		}

		/// <summary>
		/// Gets all types marked with specified attribute.
		/// </summary>
		public static IEnumerable<TypeAttributePair<TAttribute>> GetTypesWithAttribute<TAttribute>(
			Func<TypeAttributePair<TAttribute>, bool> predicate, bool observedOnly = true)
			where TAttribute : Attribute
		{
			CheckInitialized();
			var set = observedOnly ? _observedTypes : _allTypes;
			return set
				.Select(t => new TypeAttributePair<TAttribute>(t, t.GetCustomAttribute<TAttribute>(inherit: true)!))
				.Where(t => t.Attribute != null)
				.Where(t => predicate(t));
		}

		/// <summary>
		/// Gets all types inherited from specified type and marked with specified attribute.
		/// </summary>
		public static IEnumerable<TypeAttributePair<TAttribute>> GetTypesWithAttribute<TBase, TAttribute>(bool observedOnly = true)
			where TAttribute : Attribute
		{
			CheckInitialized();
			var set = observedOnly ? _observedTypes : _allTypes;
			var baseType = typeof(TBase);
			return set
				.Where(t => baseType.IsAssignableFrom(t) && t != baseType)
				.Select(t => new TypeAttributePair<TAttribute>(t, t.GetCustomAttribute<TAttribute>(inherit: true)!))
				.Where(t => t.Attribute != null);
		}

		/// <summary>
		/// Gets all types inherited from specified type and marked with specified attribute.
		/// </summary>
		public static IEnumerable<TypeAttributePair<TAttribute>> GetTypesWithAttribute<TBase, TAttribute>(
			Func<TypeAttributePair<TAttribute>, bool> predicate, bool observedOnly = true)
			where TAttribute : Attribute
		{
			CheckInitialized();
			var set = observedOnly ? _observedTypes : _allTypes;
			var baseType = typeof(TBase);
			return set
				.Where(t => baseType.IsAssignableFrom(t) && t != baseType)
				.Select(t => new TypeAttributePair<TAttribute>(t, t.GetCustomAttribute<TAttribute>(inherit: true)!))
				.Where(t => t.Attribute != null)
				.Where(t => predicate(t));
		}

		// EXTENSION METHODS

		public static IEnumerable<TypeAttributePair<TAttribute>> GetTypesWithAttribute<TAttribute>(this Assembly assembly)
			where TAttribute : Attribute
		{
			var set = assembly.GetTypes();
			return set
				.Select(t => new TypeAttributePair<TAttribute>(t, t.GetCustomAttribute<TAttribute>(inherit: true)!))
				.Where(t => t.Attribute != null);
		}

		public static IEnumerable<TypeAttributePair<TAttribute>> GetTypesWithAttribute<TAttribute>(this Assembly assembly, Func<TypeAttributePair<TAttribute>, bool> predicate)
			where TAttribute : Attribute
		{
			var set = assembly.GetTypes();
			return set
				.Select(t => new TypeAttributePair<TAttribute>(t, t.GetCustomAttribute<TAttribute>(inherit: true)!))
				.Where(t => t.Attribute != null)
				.Where(t => predicate(t));
		}

		public static IEnumerable<Type> GetTypes<TBase>(this Assembly assembly)
		{
			var set = assembly.GetTypes();
			var baseType = typeof(TBase);
			return set.Where(t => baseType.IsAssignableFrom(t) && t != baseType);
		}

		public static IEnumerable<Type> GetTypes(this Assembly assembly, Func<Type, bool> predicate)
		{
			var set = assembly.GetTypes();
			return set.Where(predicate);
		}

		public static IEnumerable<Type> AssignableFrom<TBase>(this IEnumerable<Type> types)
		{
			return types.Where(t => typeof(TBase).IsAssignableFrom(t));
		}

		public static IEnumerable<Type> InheritedFrom<TBase>(this IEnumerable<Type> types)
		{
			return types.Where(t => typeof(TBase).IsAssignableFrom(t) && t != typeof(TBase));
		}

		public static IEnumerable<Type> Implements<TBase>(this IEnumerable<Type> types)
		{
			return types.Where(t => typeof(TBase).IsAssignableFrom(t) && t != typeof(TBase) && !t.IsAbstract && !t.IsInterface);
		}

		public static IEnumerable<TypeAttributePair<TAttribute>> WithAttribute<TAttribute>(this IEnumerable<Type> types)
			where TAttribute : Attribute
		{
			return types
				.Select(t => new TypeAttributePair<TAttribute>(t, t.GetCustomAttribute<TAttribute>(inherit: true)!))
				.Where(t => t.Attribute != null);
		}

		/// <summary>
		/// Filters types in the collection to have a parameterless constructor.
		/// </summary>
		public static IEnumerable<Type> FilterParameterlessConstructors(this IEnumerable<Type> types, Action<Type> filterHandler)
		{
			foreach (var type in types)
			{
				if (type.IsAbstract || type.IsInterface)
					filterHandler(type);
				else if (type.GetConstructor(Type.EmptyTypes) is not ConstructorInfo paramLessCstr
					|| !paramLessCstr.IsPublic)
					filterHandler(type);
				else
					yield return type;
			}
		}

		/// <summary>
		/// Filters types in the collection to have a parameterless constructor.
		/// </summary>
		public static IEnumerable<TypeAttributePair<TAttr>> FilterParameterlessConstructors<TAttr>(
			this IEnumerable<TypeAttributePair<TAttr>> types, Action<Type> filterHandler)
			where TAttr : Attribute
		{
			foreach (var typePair in types)
			{
				var type = typePair.Type;
				if (type.IsAbstract || type.IsInterface)
					filterHandler(type);
				else if (type.GetConstructor(Type.EmptyTypes) is not ConstructorInfo paramLessCstr
					|| !paramLessCstr.IsPublic)
					filterHandler(type);
				else
					yield return typePair;
			}
		}

		/// <summary>
		/// Ensures that all types in the collection have a parameterless constructor. Throws an exception if any do not.
		/// </summary>
		public static IEnumerable<Type> ValidateParameterlessConstructors(this IEnumerable<Type> types)
		{
			List<Type> resultTypes;
			List<Type> errorTypes = [];

			resultTypes = types.FilterParameterlessConstructors(errorTypes.Add).ToList();

			if (errorTypes.Count > 0)
				throw new Exception($"The following types do not have a public parameterless constructor: {string.Join(", ", errorTypes.Select(t => t.FullName))}");
			
			return resultTypes;
		}

		/// <summary>
		/// Ensures that all types in the collection have a parameterless constructor. Throws an exception if any do not.
		/// </summary>
		public static IEnumerable<TypeAttributePair<TAttr>> ValidateParameterlessConstructors<TAttr>(
			this IEnumerable<TypeAttributePair<TAttr>> types)
			where TAttr : Attribute
		{
			List<TypeAttributePair<TAttr>> resultTypes;
			List<Type> errorTypes = [];

			resultTypes = types.FilterParameterlessConstructors(errorTypes.Add).ToList();

			if (errorTypes.Count > 0)
				throw new Exception($"The following types do not have a public parameterless constructor: {string.Join(", ", errorTypes.Select(t => t.FullName))}");
			
			return resultTypes;
		}

		public static object Instantiate(this Type type)
		{
			return Activator.CreateInstance(type)!;
		}

		public static object Instantiate(this Type type, params object[] args)
		{
			return Activator.CreateInstance(type, args)!;
		}

		public static T Instantiate<T>(this Type type)
		{
			return (T)Activator.CreateInstance(type)!;
		}

		public static T Instantiate<T>(this Type type, params object[] args)
		{
			return (T)Activator.CreateInstance(type, args)!;
		}

		public static IEnumerable<object> Instantiate(this IEnumerable<Type> types)
		{
			return types.Select(t => t.Instantiate());
		}

		public static IEnumerable<object> Instantiate(this IEnumerable<Type> types, params object[] args)
		{
			return types.Select(t => t.Instantiate(args));
		}

		public static IEnumerable<object> Instantiate(this IEnumerable<Type> types, Func<Type, object[]> argsProvider)
		{
			return types.Select(t => t.Instantiate(argsProvider(t)));
		}

		public static IEnumerable<T> Instantiate<T>(this IEnumerable<Type> types)
		{
			return types.Select(t => t.Instantiate<T>());
		}

		public static IEnumerable<T> Instantiate<T>(this IEnumerable<Type> types, params object[] args)
		{
			return types.Select(t => t.Instantiate<T>(args));
		}

		public static IEnumerable<T> Instantiate<T>(this IEnumerable<Type> types, Func<Type, object[]> argsProvider)
		{
			return types.Select(t => t.Instantiate<T>(argsProvider(t)));
		}

		public static IEnumerable<object> Instantiate(this IEnumerable<Type> types, Action<Type, Exception> exceptionHandler)
		{
			foreach (var type in types)
			{
				object? instance = null;
				bool success = false;
				try
				{
					instance = type.Instantiate();
					success = true;
				}
				catch (Exception ex)
				{
					exceptionHandler(type, ex);
				}
				if (success)
					yield return instance!;
			}
		}

		public static IEnumerable<object> Instantiate(this IEnumerable<Type> types, Action<Type, Exception> exceptionHandler, params object[] args)
		{
			foreach (var type in types)
			{
				object? instance = null;
				bool success = false;
				try
				{
					instance = type.Instantiate(args);
					success = true;
				}
				catch (Exception ex)
				{
					exceptionHandler(type, ex);
				}
				if (success)
					yield return instance!;
			}
		}

		public static IEnumerable<object> Instantiate(this IEnumerable<Type> types, Action<Type, Exception> exceptionHandler, Func<Type, object[]> argsProvider)
		{
			foreach (var type in types)
			{
				object? instance = null;
				bool success = false;
				try
				{
					instance = type.Instantiate(argsProvider(type));
					success = true;
				}
				catch (Exception ex)
				{
					exceptionHandler(type, ex);
				}
				if (success)
					yield return instance!;
			}
		}

		public static IEnumerable<T> Instantiate<T>(this IEnumerable<Type> types, Action<Type, Exception> exceptionHandler)
		{
			foreach (var type in types)
			{
				T? instance = default;
				bool success = false;
				try
				{
					instance = type.Instantiate<T>();
					success = true;
				}
				catch (Exception ex)
				{
					exceptionHandler(type, ex);
				}
				if (success)
					yield return instance!;
			}
		}

		public static IEnumerable<T> Instantiate<T>(this IEnumerable<Type> types, Action<Type, Exception> exceptionHandler, params object[] args)
		{
			foreach (var type in types)
			{
				T? instance = default;
				bool success = false;
				try
				{
					instance = type.Instantiate<T>(args);
					success = true;
				}
				catch (Exception ex)
				{
					exceptionHandler(type, ex);
				}
				if (success)
					yield return instance!;
			}
		}

		public static IEnumerable<T> Instantiate<T>(this IEnumerable<Type> types, Action<Type, Exception> exceptionHandler, Func<Type, object[]> argsProvider)
		{
			foreach (var type in types)
			{
				T? instance = default;
				bool success = false;
				try
				{
					instance = type.Instantiate<T>(argsProvider(type));
					success = true;
				}
				catch (Exception ex)
				{
					exceptionHandler(type, ex);
				}
				if (success)
					yield return instance!;
			}
		}
	}
}