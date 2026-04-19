using System.Numerics;
using System.Collections.Immutable;

namespace LLMDesktopAssistant.Calculation
{
	/// <summary>
	/// Represents a map of mathematical parameters to put into a mathematical function.
	/// </summary>
	public class MathParameterMap
	{
		private readonly ImmutableDictionary<string, Complex> _parameterMap;

		/// <summary>
		/// Initializes a new instance of the <see cref="MathParameterMap"/> class with a single 'x' parameter.
		/// </summary>
		/// <param name="x">The value of the 'x' parameter.</param>
		public MathParameterMap(Complex x)
		{
			var builder = ImmutableDictionary.CreateBuilder<string, Complex>();
			builder.Add("x", x);
			_parameterMap = builder.ToImmutable();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MathParameterMap"/> class with a single parameter.
		/// </summary>
		/// <param name="key">The parameter key.</param>
		/// <param name="value">The parameter value.</param>
		public MathParameterMap(string key, Complex value)
		{
			var builder = ImmutableDictionary.CreateBuilder<string, Complex>();
			builder.Add(key, value);
			_parameterMap = builder.ToImmutable();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MathParameterMap"/> class with multiple parameters.
		/// </summary>
		/// <param name="parameters">A collection of parameters to add to the map.</param>
		public MathParameterMap(IEnumerable<KeyValuePair<string, Complex>> parameters)
		{
			var builder = ImmutableDictionary.CreateBuilder<string, Complex>();
			builder.AddRange(parameters);
			_parameterMap = builder.ToImmutable();
		}

		/// <summary>
		/// Creates a new instance of the <see cref="MathParameterMap"/> class with one parameter and replaces an existing one if it exists.
		/// </summary>
		/// <param name="key">The key to replace.</param>
		/// <param name="newValue">The new value to set for the key.</param>
		/// <returns>A new instance with the replaced parameter.</returns>
		public MathParameterMap WithReplaced(string key, Complex newValue)
		{
			return new MathParameterMap(_parameterMap.SetItem(key, newValue));
		}

		/// <summary>
		/// Gets the value of a parameter by its key. If the key does not exist, returns 0+0i.
		/// </summary>
		/// <param name="key">The parameter key.</param>
		/// <returns>The value of the parameter. If the key does not exist, returns 0+0i.</returns>
		public Complex this[string key] => _parameterMap.TryGetValue(key, out var value) ? value : new Complex(0, 0);
	}
}