using LLMDesktopAssistant.Data;
using LLMDesktopAssistant.Data.ChatModels;
using LLMDesktopAssistant.LLM.MVVM.Additional;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Services.Storage
{
	/// <summary>
	/// Synchronizes one additional view model with its <see cref="AdditionalMessageViewDataModel"/> row.
	/// Created for every additional view model that belongs to a message, a tool call or the chat itself.
	/// </summary>
	public class AdditionalViewModelDatabaseSynchronizer : Disposable
	{
		private readonly ChatDatabase _database;
		private readonly AdditionalMessageViewModel _target;
		private readonly ChangeTracker _changeTracker;
		private AdditionalMessageViewDataModel? _model;

		public AdditionalViewModelDatabaseSynchronizer(ChatDatabase database,
			AdditionalMessageViewModel target, AdditionalMessageViewDataModel? model,
			VMParentKind parentKind, int parentId)
		{
			_database = database;
			_target = target;

			bool prevTemporary = target.IsTemporary;
			_model = model ?? database.AdditionalMessageViewModels
				.FindOne(avm => avm.ParentKind == parentKind && avm.ParentId == parentId && avm.ViewModel.Guid == target.Guid);

			if (_model == null && !prevTemporary)
			{
				_model = new AdditionalMessageViewDataModel
				{
					ParentKind = parentKind,
					ParentId = parentId,
					ViewModel = target
				};
				_database.AdditionalMessageViewModels.Insert(_model);
			}

			_changeTracker = new ChangeTracker(target, () =>
			{
				if (prevTemporary != target.IsTemporary)
				{
					if (prevTemporary) // Became persistent
					{
						_model = new AdditionalMessageViewDataModel
						{
							ParentKind = parentKind,
							ParentId = parentId,
							ViewModel = target
						};
						_database.AdditionalMessageViewModels.Insert(_model);
					}
					else // Became temporary
					{
						if (_model != null)
							_database.AdditionalMessageViewModels.Delete(_model.Id);
						_model = null;
					}
					prevTemporary = target.IsTemporary;
				}
				else if (_model != null)
					_database.AdditionalMessageViewModels.Update(_model);
			});
		}

		/// <summary>
		/// Removes the view model row from the database and disposes this synchronizer.
		/// </summary>
		public void Delete()
		{
			if (_model != null)
				_database.AdditionalMessageViewModels.Delete(_model.Id);

			Dispose();
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
			{
				// Dispose target or not?
				// _target.Dispose();
				_changeTracker.Dispose();
			}
		}
	}
}
