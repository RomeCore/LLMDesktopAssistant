using System.Collections.Specialized;
using LLMDesktopAssistant.Addons.Management;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Addons
{
	public class AddonAccessor<T> : Disposable, IAddonAccessor<T>
	{
		private readonly RangeObservableCollection<T> _addons = [];
		private readonly IReactiveAddonLoader<T>[] _loaders;
		private readonly IAddonManagerInvalidator _invalidator;

		public ReadOnlyObservableCollection<T> Addons
		{
			get
			{
				_invalidator.ReloadIfInvalid();
				return field ??= new(_addons);
			}
		}

		public AddonAccessor(IEnumerable<IReactiveAddonLoader<T>> loaders,
			IAddonManagerInvalidator invalidator)
		{
			_loaders = [.. loaders];
			_invalidator = invalidator;

			foreach (var loader in _loaders)
			{
				loader.Addons.CollectionChanged += Addons_CollectionChanged;
				Addons_CollectionChanged(null, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, loader.Addons));
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
				_addons.Clear();
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
