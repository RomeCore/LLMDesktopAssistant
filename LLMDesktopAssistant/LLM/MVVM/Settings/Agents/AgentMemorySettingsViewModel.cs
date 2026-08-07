using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Agents.Memory;
using LLMDesktopAssistant.Data;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.Agents
{
	/// <summary>
	/// Represents a <see cref="MemoryBlockAttachmentMode"/> value with a localized display name for use in ComboBox.
	/// </summary>
	public class MemoryBlockAttachmentModeItem
	{
		/// <summary>
		/// Gets the <see cref="MemoryBlockAttachmentMode"/> value.
		/// </summary>
		public MemoryBlockAttachmentMode Value { get; }

		/// <summary>
		/// Gets the localized display name.
		/// </summary>
		public string DisplayName { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="MemoryBlockAttachmentModeItem"/> class.
		/// </summary>
		/// <param name="value">The attachment mode value.</param>
		public MemoryBlockAttachmentModeItem(MemoryBlockAttachmentMode value)
		{
			Value = value;
			var key = $"memory_attachment_mode_{value.ToString().ToLower()}";
			DisplayName = LocalizationManager.LocalizeStatic(key);

			// Fallback to enum name if localization missing
			if (DisplayName == key || string.IsNullOrEmpty(DisplayName))
				DisplayName = value.ToString();
		}

		/// <summary>
		/// Gets all <see cref="MemoryBlockAttachmentMode"/> values with localized display names.
		/// </summary>
		public static ImmutableList<MemoryBlockAttachmentModeItem> All { get; } =
			Enum.GetValues<MemoryBlockAttachmentMode>()
				.Select(v => new MemoryBlockAttachmentModeItem(v))
				.ToImmutableList();
	}

	/// <summary>
	/// ViewModel item for a single memory block attached to the agent.
	/// Provides enabled toggle, attachment mode selector and a remove command.
	/// </summary>
	public class MemoryBlockAttachmentItemViewModel : ViewModelBase
	{
		private readonly MemoryBlockAttachment _attachment;
		private readonly RangeObservableCollection<MemoryBlockAttachment> _attachments;
		private readonly Action? _removed;

		/// <summary>
		/// Gets the identifier of the referenced memory block.
		/// </summary>
		public string BlockId => _attachment.Reference.Id;

		/// <summary>
		/// Gets the resolved memory block, or <see langword="null"/> when the referenced block does not exist.
		/// </summary>
		public MemoryBlock? Block => _attachment.Reference.Object;

		/// <summary>
		/// Gets the display name of the attached block.
		/// </summary>
		public string Name => Block?.Name ?? string.Format(LocalizationManager.LocalizeStatic("settings-memory_missing_block"), BlockId);

		/// <summary>
		/// Gets the description of the attached block.
		/// </summary>
		public string? Description => Block?.Description;

		/// <summary>
		/// Gets the list of available attachment modes for the ComboBox.
		/// </summary>
		public ImmutableList<MemoryBlockAttachmentModeItem> ModeList { get; } = MemoryBlockAttachmentModeItem.All;

		/// <summary>
		/// Gets the command that detaches the block from the agent.
		/// </summary>
		public ICommand RemoveCommand { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="MemoryBlockAttachmentItemViewModel"/> class.
		/// </summary>
		/// <param name="attachment">The attachment being edited.</param>
		/// <param name="attachments">The collection the attachment belongs to.</param>
		/// <param name="removed">An optional callback invoked after the attachment is removed.</param>
		public MemoryBlockAttachmentItemViewModel(
			MemoryBlockAttachment attachment,
			RangeObservableCollection<MemoryBlockAttachment> attachments,
			Action? removed = null)
		{
			_attachment = attachment;
			_attachments = attachments;
			_removed = removed;
			RemoveCommand = new RelayCommand(Remove);

			if (attachment.Reference.Object is { } block)
				block.PropertyChanged += Block_PropertyChanged;
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (_attachment.Reference.Object is { } block)
				block.PropertyChanged -= Block_PropertyChanged;
		}

		/// <summary>
		/// Gets or sets whether this memory block attachment is enabled.
		/// </summary>
		public bool Enabled
		{
			get => _attachment.Enabled;
			set
			{
				if (_attachment.Enabled != value)
				{
					_attachment.Enabled = value;
					RaisePropertyChanged();
				}
			}
		}

		/// <summary>
		/// Gets or sets the attachment mode for this memory block.
		/// </summary>
		public MemoryBlockAttachmentModeItem? Mode
		{
			get => ModeList.FirstOrDefault(m => m.Value == _attachment.Mode);
			set
			{
				if (value != null && _attachment.Mode != value.Value)
				{
					_attachment.Mode = value.Value;
					RaisePropertyChanged();
				}
			}
		}

		private void Block_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(MemoryBlock.Name) or nameof(MemoryBlock.Description))
			{
				RaisePropertyChanged(nameof(Name));
				RaisePropertyChanged(nameof(Description));
			}
		}

		private void Remove()
		{
			_attachments.Remove(_attachment);
			_removed?.Invoke();
		}
	}

	/// <summary>
	/// ViewModel for the per-agent memory settings tab.
	/// The attached blocks are resolved through the effective (inherited) scope, while
	/// the block editor below operates on the shared <see cref="MemoryBlock"/> instances
	/// stored in the memory blocks settings category.
	/// </summary>
	[ViewModelFor(typeof(AgentMemorySettingsView))]
	public class AgentMemorySettingsViewModel : ViewModelBase
	{
		private readonly ChatSettings _chatSettings;
		private readonly IMemoryDatabaseManager _databaseManager;

		/// <summary>
		/// Gets the underlying agent memory settings.
		/// </summary>
		public AgentMemorySettings MemorySettings { get; }

		/// <summary>
		/// Gets the effective attached blocks resolved by the current inheritance level.
		/// </summary>
		public RangeObservableCollection<MemoryBlockAttachment> EffectiveBlocks => MemorySettings.GetEffectiveBlocks(_chatSettings);

		/// <summary>
		/// Gets the settings category that stores the shared memory block definitions.
		/// </summary>
		public static SettingsCategory<MemoryBlock> BlocksCategory { get; } = SettingsManager.GetCategory<MemoryBlock>();

		/// <summary>
		/// Gets the available shared memory block IDs for attaching existing blocks.
		/// </summary>
		public RangeObservableCollection<SettingsIdItemViewModel> AvailableBlockIds { get; } =
			[.. BlocksCategory.GetAvailableIds()
				.Where(c => c != SettingsObject.DefaultId)
				.Select(c => new SettingsIdItemViewModel { Id = c })];

		private SettingsIdItemViewModel? _selectedAvailableBlockId;
		/// <summary>
		/// Gets or sets the memory block selected in the "attach existing" combo box.
		/// </summary>
		public SettingsIdItemViewModel? SelectedAvailableBlockId
		{
			get => _selectedAvailableBlockId;
			set => SetProperty(ref _selectedAvailableBlockId, value);
		}

		private RangeObservableCollection<MemoryBlockAttachmentItemViewModel> _blockItems = [];
		/// <summary>
		/// Gets or sets the list of attached memory block items.
		/// </summary>
		public ICollection<MemoryBlockAttachmentItemViewModel> BlockItems
		{
			get => _blockItems;
			set
			{
				_blockItems.Reset(value);
				RaisePropertyChanged(nameof(BlockItems));
			}
		}

		private MemoryBlockAttachmentItemViewModel? _selectedBlockItem;
		/// <summary>
		/// Gets or sets the selected attached block. The block editor below is bound to it.
		/// </summary>
		public MemoryBlockAttachmentItemViewModel? SelectedBlockItem
		{
			get => _selectedBlockItem;
			set
			{
				if (SetProperty(ref _selectedBlockItem, value))
				{
					RaisePropertyChanged(nameof(EditedBlock));
					RaisePropertyChanged(nameof(HasEditedBlock));
					RaisePropertyChanged(nameof(EditedBlockId));
					DuplicateBlockCommand.NotifyCanExecuteChanged();
					RenameBlockCommand.NotifyCanExecuteChanged();
					DeleteBlockCommand.NotifyCanExecuteChanged();
				}
			}
		}

		/// <summary>
		/// Gets the memory block being edited in the block editor below.
		/// </summary>
		public MemoryBlock? EditedBlock => SelectedBlockItem?.Block;

		/// <summary>
		/// Gets a value indicating whether a block is selected and can be edited.
		/// </summary>
		public bool HasEditedBlock => EditedBlock != null;

		/// <summary>
		/// Gets the identifier of the edited block.
		/// </summary>
		public string EditedBlockId => EditedBlock?.Id ?? string.Empty;

		private InheritanceLevelItem _selectedBlocksInheritance;
		/// <summary>
		/// Gets or sets the inheritance level for the attached blocks group.
		/// </summary>
		public InheritanceLevelItem SelectedBlocksInheritance
		{
			get => _selectedBlocksInheritance;
			set
			{
				if (SetProperty(ref _selectedBlocksInheritance, value) && value != null)
					MemorySettings.BlocksInheritance = value.Value;
			}
		}

		private bool _isEditingId;
		/// <summary>
		/// Gets or sets a value indicating whether the block ID rename editor is visible.
		/// </summary>
		public bool IsEditingId
		{
			get => _isEditingId;
			set => SetProperty(ref _isEditingId, value);
		}

		private string? _newId;
		/// <summary>
		/// Gets or sets the block ID being entered for the rename operation.
		/// </summary>
		public string? NewId
		{
			get => _newId;
			set => SetProperty(ref _newId, value);
		}

		/// <summary>
		/// Gets the command that creates a new memory block and attaches it to the agent.
		/// </summary>
		public ICommand AddBlockCommand { get; }

		/// <summary>
		/// Gets the command that attaches the block selected in the combo box to the agent.
		/// </summary>
		public ICommand AttachBlockCommand { get; }

		/// <summary>
		/// Gets the command that duplicates the edited block and attaches the copy to the agent.
		/// </summary>
		public AsyncRelayCommand DuplicateBlockCommand { get; }

		/// <summary>
		/// Gets the command that starts renaming the edited block.
		/// </summary>
		public RelayCommand RenameBlockCommand { get; }

		/// <summary>
		/// Gets the command that deletes the edited block together with its attachments.
		/// </summary>
		public AsyncRelayCommand DeleteBlockCommand { get; }

		/// <summary>
		/// Gets the command that confirms the block rename.
		/// </summary>
		public AsyncRelayCommand ConfirmEditIdCommand { get; }

		/// <summary>
		/// Gets the command that cancels the block rename.
		/// </summary>
		public ICommand CancelEditIdCommand { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="AgentMemorySettingsViewModel"/> class.
		/// </summary>
		/// <param name="settings">The agent memory settings to edit.</param>
		/// <param name="chatSettings">The chat settings used to resolve inherited settings.</param>
		/// <param name="databaseManager">The memory database manager used for block database operations.</param>
		public AgentMemorySettingsViewModel(AgentMemorySettings settings, ChatSettings chatSettings, IMemoryDatabaseManager databaseManager)
		{
			MemorySettings = settings;
			_chatSettings = chatSettings;
			_databaseManager = databaseManager;

			_selectedBlocksInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == settings.BlocksInheritance);
			settings.PropertyChanged += MemorySettings_PropertyChanged;

			UpdateBlocks();

			AddBlockCommand = new RelayCommand(AddBlock);
			AttachBlockCommand = new RelayCommand(AttachBlock);
			DuplicateBlockCommand = new AsyncRelayCommand(DuplicateBlockAsync, () => HasEditedBlock);
			RenameBlockCommand = new RelayCommand(RenameBlock, () => HasEditedBlock);
			DeleteBlockCommand = new AsyncRelayCommand(DeleteBlockAsync, () => HasEditedBlock);
			ConfirmEditIdCommand = new AsyncRelayCommand(ConfirmRenameBlockAsync);
			CancelEditIdCommand = new RelayCommand(() => IsEditingId = false);
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			MemorySettings.PropertyChanged -= MemorySettings_PropertyChanged;
			DisposeItems();
		}

		private void MemorySettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != nameof(AgentMemorySettings.Blocks))
				return;

			_selectedBlocksInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == MemorySettings.BlocksInheritance);
			RaisePropertyChanged(nameof(SelectedBlocksInheritance));
			RaisePropertyChanged(nameof(EffectiveBlocks));
			UpdateBlocks();
		}

		private void DisposeItems()
		{
			foreach (var item in _blockItems)
				item.Dispose();
		}

		private void UpdateBlocks()
		{
			DisposeItems();
			SelectedBlockItem = null;

			var items = new List<MemoryBlockAttachmentItemViewModel>(EffectiveBlocks.Count);
			foreach (var attachment in EffectiveBlocks)
				items.Add(new MemoryBlockAttachmentItemViewModel(attachment, EffectiveBlocks, UpdateBlocks));

			BlockItems = items;
		}

		private void AddBlock()
		{
			var id = GenerateBlockId();
			var block = BlocksCategory.Get(id);
			block.Name = LocalizationManager.LocalizeStatic("settings-memory_default_block_name");

			AvailableBlockIds.Add(new SettingsIdItemViewModel { Id = id });
			Attach(id);
		}

		private void AttachBlock()
		{
			if (SelectedAvailableBlockId is not { } item)
				return;

			Attach(item.Id);
		}

		private void Attach(string id)
		{
			var existing = BlockItems.FirstOrDefault(b => b.BlockId == id);
			if (existing != null)
			{
				SelectedBlockItem = existing;
				return;
			}

			EffectiveBlocks.Add(new MemoryBlockAttachment
			{
				Reference = new SettingsReference<MemoryBlock> { Id = id }
			});

			UpdateBlocks();
			SelectedBlockItem = BlockItems.FirstOrDefault(b => b.BlockId == id);
		}

		private async Task DuplicateBlockAsync()
		{
			if (EditedBlock is not { } block)
				return;

			var id = GenerateBlockId();
			if (!BlocksCategory.Copy(block.Id, id))
				return;

			var copy = BlocksCategory.Get(id);
			copy.Name = block.Name + " (" + LocalizationManager.LocalizeStatic("settings-memory_copy_suffix") + ")";

			await _databaseManager.CopyAsync(block.Id, id);

			AvailableBlockIds.Add(new SettingsIdItemViewModel { Id = id });
			Attach(id);
		}

		private void RenameBlock()
		{
			if (EditedBlock is not { } block)
				return;

			NewId = block.Id;
			IsEditingId = true;
		}

		private async Task ConfirmRenameBlockAsync()
		{
			if (EditedBlock is not { } block)
			{
				IsEditingId = false;
				return;
			}

			var oldId = block.Id;
			var newId = NewId?.Trim();
			if (string.IsNullOrWhiteSpace(newId) || newId == oldId)
			{
				IsEditingId = false;
				return;
			}

			if (!BlocksCategory.Rename(oldId, newId))
				return;

			foreach (var attachment in EffectiveBlocks.Where(b => b.Reference.Id == oldId))
				attachment.Reference.Id = newId;

			if (oldId != SettingsObject.DefaultId)
				AvailableBlockIds.Remove(new SettingsIdItemViewModel { Id = oldId });
			if (newId != SettingsObject.DefaultId && !AvailableBlockIds.Any(i => i.Id == newId))
				AvailableBlockIds.Add(new SettingsIdItemViewModel { Id = newId });

			UpdateBlocks();
			SelectedBlockItem = BlockItems.FirstOrDefault(b => b.BlockId == newId);
			IsEditingId = false;

			await _databaseManager.RenameAsync(oldId, newId);
		}

		private async Task DeleteBlockAsync()
		{
			if (EditedBlock is not { } block)
				return;

			var id = block.Id;
			foreach (var attachment in EffectiveBlocks.Where(b => b.Reference.Id == id).ToList())
				EffectiveBlocks.Remove(attachment);

			BlocksCategory.Remove(id);

			if (id != SettingsObject.DefaultId)
				AvailableBlockIds.Remove(new SettingsIdItemViewModel { Id = id });

			UpdateBlocks();
			SelectedBlockItem = null;

			await _databaseManager.DeleteAsync(id);
		}

		private static string GenerateBlockId()
		{
			var taken = BlocksCategory.GetAvailableIds().ToHashSet();
			int i = 1;
			while (taken.Contains($"block-{i}"))
				i++;
			return $"block-{i}";
		}
	}
}
