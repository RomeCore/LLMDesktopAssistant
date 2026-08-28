using System.Collections;

namespace LLMDesktopAssistant.Utils
{
	/// <summary>
	/// An append-only list. This list can only be appended to, not modified or removed from.
	/// </summary>
	/// <typeparam name="T">The type of elements in the list.</typeparam>
	public class AppendOnlyList<T> : IEnumerable<T>
	{
		private readonly List<T> _list = [];

		/// <summary>
		/// Gets the number of elements contained in the list.
		/// </summary>
		public int Count => _list.Count;

		/// <summary>
		/// Initializes a new instance of the <see cref="AppendOnlyList{T}"/> class.
		/// </summary>
		public AppendOnlyList()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AppendOnlyList{T}"/> class with the elements of the specified collection.
		/// </summary>
		/// <param name="collection">The collection whose elements are copied to the new list.</param>
		public AppendOnlyList(IEnumerable<T> collection)
		{
			_list.AddRange(collection);
		}

		/// <summary>
		/// Appends an item to the end of the list.
		/// </summary>
		/// <param name="item">The object to append to the list.</param>
		public void Append(T item)
		{
			_list.Add(item);
		}

		/// <summary>
		/// Appends a range of items to the end of the list.
		/// </summary>
		/// <param name="items">The collection of objects to append to the list.</param>
		public void AppendRange(IEnumerable<T> items)
		{
			_list.AddRange(items);
		}

		public IEnumerator<T> GetEnumerator()
		{
			return ((IEnumerable<T>)_list).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)_list).GetEnumerator();
		}
	}
}
