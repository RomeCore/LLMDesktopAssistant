using System.Collections.Specialized;
using System.ComponentModel;
using LLMDesktopAssistant.Data;
using LLMDesktopAssistant.Data.ChatModels;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.MVVM.Additional;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Services.Storage
{
	/// <summary>
	/// Synchronizes one tool call (domain object) with its <see cref="ToolCallModel"/> row and
	/// with the additional view models nested inside it (<see cref="ChatObjectBase.AdditionalViewModels"/>).
	/// Created per tool call by <see cref="MessageDatabaseSynchronizer"/>.
	/// </summary>
	public class ToolCallDatabaseSynchronizer : Disposable
	{
		private readonly ChatDatabase _database;
		private readonly ToolCallModel _model;

		/// <summary>
		/// The domain object being synchronized.
		/// </summary>
		public ToolCall Target { get; }

		/// <summary>
		/// Nested synchronizers for additional view models, keyed by the domain view model.
		/// Created/destroyed when <see cref="ChatObjectBase.AdditionalViewModels"/> collection changes.
		/// </summary>
		private readonly Dictionary<AdditionalMessageViewModel, AdditionalViewModelDatabaseSynchronizer> _additionalViewModels = [];

		/// <summary>
		/// Creates a synchronizer for a tool call. When <paramref name="model"/> is null - the tool call
		/// is brand new: the model is built from the domain object, inserted and <see cref="ChatObjectBase.Id"/>
		/// is assigned from the generated ID. When a model is provided (loaded from database) - only
		/// subscriptions are made. Existing additional view models are picked up either way.
		/// </summary>
		public ToolCallDatabaseSynchronizer(ChatDatabase database, ToolCall target, int messageId, ToolCallModel? model)
		{
			_database = database;
			Target = target;

			if (model is not null)
			{
				_model = model;
			}
			else if (target.Id != 0 && database.ToolCalls.FindById(target.Id) is { } existing)
			{
				// Loaded from the database: the row already exists, find it by the domain ID.
				_model = existing;
			}
			else
			{
				// Brand new tool call: create the row and assign the generated ID.
				_model = CreateModelAndInsert(target, messageId);
				target.Id = _model.Id;
			}

			foreach (var vm in target.AdditionalViewModels)
				_additionalViewModels[vm] = CreateAdditionalViewModelSynchronizer(vm);

			target.PropertyChanged += OnTargetPropertyChanged;
			target.AdditionalViewModels.CollectionChanged += OnAdditionalViewModelsCollectionChanged;
		}

		/// <summary>
		/// Removes the tool call row and all its additional view model rows from the database.
		/// Disposes this synchronizer.
		/// </summary>
		public void Delete()
		{
			foreach (var vmSync in _additionalViewModels.Values)
				vmSync.Delete();
			_database.ToolCalls.Delete(_model.Id);

			Dispose();
		}

		/// <summary>
		/// Creates an additional view model synchronizer for a nested view model.
		/// Passes a null model: the synchronizer will find an existing row by the view model GUID
		/// (when the view model was loaded from the database) or insert a new row otherwise.
		/// </summary>
		private AdditionalViewModelDatabaseSynchronizer CreateAdditionalViewModelSynchronizer(AdditionalMessageViewModel vm)
			=> new(_database, vm, model: null, VMParentKind.ToolCall, _model.Id);

		private void OnTargetPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			CopyToModel(_model, Target);
			_database.ToolCalls.Update(_model);
		}

		private void OnAdditionalViewModelsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.OldItems != null)
				foreach (AdditionalMessageViewModel oldVm in e.OldItems)
				{
					if (_additionalViewModels.TryGetValue(oldVm, out var sync))
					{
						sync.Delete();
						_additionalViewModels.Remove(oldVm);
					}
				}

			if (e.NewItems != null)
				foreach (AdditionalMessageViewModel newVm in e.NewItems)
					_additionalViewModels[newVm] = CreateAdditionalViewModelSynchronizer(newVm);
		}

		private void CopyToModel(ToolCallModel model, ToolCall from)
		{
			model.ToolCallId = Target.ToolCallId;
			model.ToolName = Target.ToolName;
			model.Title = Target.Title;
			model.FunctionArguments = Target.Arguments;
			model.Status = Target.Status switch
			{
				ToolStatus.None => ToolStatusModel.NotExecuted,
				ToolStatus.Executing => ToolStatusModel.ExecutionBegin,
				ToolStatus.Success => ToolStatusModel.Success,
				ToolStatus.Error => ToolStatusModel.Error,
				ToolStatus.Cancelled => ToolStatusModel.Cancelled,
				_ => ToolStatusModel.NotExecuted,
			};
			model.StatusIcon = Target.StatusIcon;
			model.StatusTitle = Target.StatusTitle;
			model.ExpectedBehaviour = Target.ExpectedBehaviour;
			model.ResultContent = Target.ResultContent;
			model.UseMarkdown = Target.UseMarkdown;
			model.StructuredResult = Target.StructuredResult?.ToJsonString();
		}

		private ToolCallModel CreateModelAndInsert(ToolCall toolCall, int messageId)
		{
			var model = new ToolCallModel
			{
				MessageId = messageId
			};
			CopyToModel(model, toolCall);
			_database.ToolCalls.Insert(model);
			return model;
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
			{
				Target.PropertyChanged -= OnTargetPropertyChanged;
				Target.AdditionalViewModels.CollectionChanged -= OnAdditionalViewModelsCollectionChanged;

				// Dispose nested synchronizers WITHOUT deleting rows - the tool call stays in the database.
				foreach (var vmSync in _additionalViewModels.Values)
					vmSync.Dispose();
			}
		}
	}
}
