using DocumentFormat.OpenXml.Bibliography;
using LLMDesktopAssistant.Utils;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;

namespace LLMDesktopAssistant.LLM.Domain
{
	public class AdditionalMessageViewModelCollection : RangeObservableOrderedCollection<AdditionalMessageViewModel>
	{ 
		public AdditionalMessageViewModelCollection()
		{
			RaiseInUIThread = true;
			Comparer = AdditionalViewModelComparer.Instance;
		}

		/// <summary>
		/// Tries to get the first additional view model of a specific type.
		/// </summary>
		/// <typeparam name="T">The type of the additional view model to get.</typeparam>
		/// <returns>The first additional view model of the specified type, or null if none is found.</returns>
		public bool Has<T>() where T : AdditionalMessageViewModel
		{
			lock (SyncRoot)
				for (int i = 0; i < Items.Count; i++)
					if (Items[i] is T)
						return true;
			return false;
		}

		/// <summary>
		/// Tries to get the first additional view model of a specific type.
		/// </summary>
		/// <typeparam name="T">The type of the additional view model to get.</typeparam>
		/// <returns>The first additional view model of the specified type, or null if none is found.</returns>
		public T? TryGet<T>() where T : AdditionalMessageViewModel
		{
			lock (SyncRoot)
				for (int i = 0; i < Items.Count; i++)
					if (Items[i] is T viewModel)
						return viewModel;
			return null;
		}

		/// <summary>
		/// Tries to get the first additional view model of a specific type.
		/// </summary>
		/// <typeparam name="T">The type of the additional view model to get.</typeparam>
		/// <param name="viewModel">The output parameter that will contain the first additional view model of the specified type, or null if none is found.</param>
		/// <returns>True if the first additional view model of the specified type was found and assigned to the output parameter, otherwise false.</returns>
		public bool TryGet<T>([NotNullWhen(true)] out T viewModel) where T : AdditionalMessageViewModel
		{
			lock (SyncRoot)
				for (int i = 0; i < Items.Count; i++)
					if (Items[i] is T _viewModel)
					{
						viewModel = _viewModel;
						return true;
					}
			viewModel = null!;
			return false;
		}

		/// <summary>
		/// Gets all additional view models of a specific type.
		/// </summary>
		/// <typeparam name="T">The type of the additional view models to get.</typeparam>
		/// <returns>A collection of all additional view models of the specified type.</returns>
		public List<T> GetAll<T>() where T : AdditionalMessageViewModel
		{
			var list = new List<T>();
			lock (SyncRoot)
				for (int i = 0; i < Items.Count; i++)
					if (Items[i] is T viewModel)
						list.Add(viewModel);
			return list;
		}

		/// <summary>
		/// Replaces the first additional view model of a specific type with a new one.
		/// If not found, the new view model will be added to the collection.
		/// </summary>
		/// <typeparam name="T">The type of the additional view model to replace.</typeparam>
		/// <param name="viewModel">The new additional view model to replace the existing one with.</param>
		public void TryReplace<T>(T viewModel) where T : AdditionalMessageViewModel
		{
			int replaceIndex = -1;

			lock (SyncRoot)
			{
				for (int i = 0; i < Items.Count; i++)
					if (Items[i] is T oldItem)
					{
						replaceIndex = i;
						break;
					}
			}

			if (replaceIndex != -1)
				Set(replaceIndex, viewModel);
			else
				Add(viewModel);
		}
	}
}