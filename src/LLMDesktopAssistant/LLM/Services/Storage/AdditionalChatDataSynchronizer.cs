using LLMDesktopAssistant.Data;
using LLMDesktopAssistant.Data.ChatModels;
using LLMDesktopAssistant.LLM.MVVM.Additional;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Services.Storage
{
	/// <summary>
	/// Synchronizes one additional view model with its <see cref="AdditionalChatDataModel"/> row.
	/// Created for every additional view model that belongs to a message, a tool call or the chat itself.
	/// </summary>
	public class AdditionalChatDataSynchronizer : Disposable
	{
		private readonly ChatDatabase _database;
		private readonly ChangeTracker _changeTracker;
		private AdditionalChatDataModel? _model;

		/// <summary>
		/// Gets the target object that this synchronizer persists to the database on changes.
		/// </summary>
		public AdditionalChatData Target { get; }

		public static AdditionalChatDataSynchronizer FromTarget(ChatDatabase database,
			AdditionalChatData target, ChatDataParentKind parentKind, int parentId)
		{
			AdditionalChatDataModel? model = null;
			if (!target.IsTemporary)
			{
				model = new AdditionalChatDataModel
				{
					ParentKind = parentKind,
					ParentId = parentId,
					Data = target
				};
				database.AdditionalChatData.Insert(model);
			}
			return new AdditionalChatDataSynchronizer(database, target, model, parentKind, parentId);
		}

		public static AdditionalChatDataSynchronizer FromModel(ChatDatabase database,
			AdditionalChatDataModel model)
		{
			return new AdditionalChatDataSynchronizer(database, model.Data, model, model.ParentKind, model.ParentId);
		}

		private AdditionalChatDataSynchronizer(ChatDatabase database,
			AdditionalChatData target, AdditionalChatDataModel? model, ChatDataParentKind parentKind, int parentId)
		{
			_database = database;
			Target = target;
			_model = model;

			bool prevTemporary = target.IsTemporary;

			_changeTracker = new ChangeTracker(target, () =>
			{
				if (prevTemporary != target.IsTemporary)
				{
					if (prevTemporary) // Became persistent
					{
						_model = new AdditionalChatDataModel
						{
							ParentKind = parentKind,
							ParentId = parentId,
							Data = target
						};
						_database.AdditionalChatData.Insert(_model);
					}
					else // Became temporary
					{
						if (_model != null)
							_database.AdditionalChatData.Delete(_model.Id);
						_model = null;
					}
					prevTemporary = target.IsTemporary;
				}
				else if (_model != null)
					_database.AdditionalChatData.Update(_model);
			});
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
			{
				_changeTracker.Dispose();
			}
		}

		/// <summary>
		/// Removes the view model row from the database and disposes this synchronizer.
		/// </summary>
		public void Delete()
		{
			ThrowIfDisposed();

			if (_model != null)
				_database.AdditionalChatData.Delete(_model.Id);

			Dispose();
		}
	}
}
