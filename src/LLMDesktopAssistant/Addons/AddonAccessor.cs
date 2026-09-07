using System.Collections.Specialized;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Addons
{
	[Service(typeof(IAddonAccessor<>))]
	[ChatService(typeof(IAddonAccessor<>))]
	public class AddonAccessor<T> : Disposable, IAddonAccessor<T>
	{
		private readonly RangeObservableCollection<T> _addons = [];
		private readonly IReactiveAddonLoader<T>[] _loaders;

		public ReadOnlyObservableCollection<T> Addons => field ??= new(_addons);

		public AddonAccessor(IEnumerable<IReactiveAddonLoader<T>> loaders)
		{
			_loaders = [.. loaders];

			foreach (var loader in _loaders)
			{
				loader.Addons.CollectionChanged += Addons_CollectionChanged;
			}
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
			{
				foreach (var loader in _loaders)
				{
					loader.Addons.CollectionChanged -= Addons_CollectionChanged;
				}
			}
		}

		private void Addons_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action is NotifyCollectionChangedAction.Move)
				return;

			if (e.OldItems is not null)
				foreach (var _item in e.OldItems)
					if (_item is T item)
						_addons.Remove(item);

			if (e.NewItems is not null)
				foreach (var _item in e.NewItems)
					if (_item is T item)
						_addons.Add(item);
		}
	}
}
