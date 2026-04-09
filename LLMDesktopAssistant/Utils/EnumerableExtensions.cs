using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.Utils
{
	public static class EnumerableExtensions
	{
		/// <summary>
		/// Returns the first element of a sequence of rest elements are equal to it.
		/// Returns default value if collection is empty or elements are not equal.
		/// </summary>
		public static T? GetAllEqualOrDefault<T>(this IEnumerable<T> source,
			T? defaultValue = default, IEqualityComparer<T>? comparer = null)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			comparer ??= EqualityComparer<T>.Default;

			using var enumerator = source.GetEnumerator();

			if (!enumerator.MoveNext())
				return defaultValue;

			T first = enumerator.Current;

			while (enumerator.MoveNext())
			{
				if (!comparer.Equals(enumerator.Current, first))
					return defaultValue;
			}

			return first;
		}
	}
}