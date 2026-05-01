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

		/// <summary>
		/// Removes consecutive duplicates from a sequence. <br/>
		/// Example: [1, 2, 2, 3, 4, 4, 5, 4, 4, 0, 0, 0] -&gt; [1, 2, 3, 4, 5, 4, 0] <br/>
		/// </summary>
		/// <typeparam name="T">The type of the elements in the sequence.</typeparam>
		/// <param name="source">The sequence to remove duplicates from.</param>
		/// <param name="comparer">The equality comparer to use for comparing elements. If null, the default equality comparer is used.</param>
		/// <returns>A new sequence with consecutive duplicates removed.</returns>
		public static IEnumerable<T> RemoveConsecutiveDuplicates<T>(this IEnumerable<T> source,
			IEqualityComparer<T>? comparer = null)
		{
			using var enumerator = source.GetEnumerator();
			if (!enumerator.MoveNext())
				yield break;

			comparer ??= EqualityComparer<T>.Default;

			T previous = enumerator.Current;
			yield return previous;

			while (enumerator.MoveNext())
			{
				if (!comparer.Equals(enumerator.Current, previous))
				{
					previous = enumerator.Current;
					yield return previous;
				}
			}
		}
	}
}