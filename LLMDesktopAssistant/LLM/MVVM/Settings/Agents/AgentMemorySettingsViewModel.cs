using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Agents.Memory;
using LLMDesktopAssistant.Controls.Dialogs;
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
	/// ViewModel item for a shared memory block in the "attach existing block" combo box.
	/// Shows the block name with the block ID as a dimmed subtitle.
	/// </summary>
	public class MemoryBlockIdItemViewModel : ViewModelBase
	{
		private readonly MemoryBlock _block;

		/// <summary>
		/// Gets the identifier of the memory block.
		/// </summary>
		public string Id { get; }

		/// <summary>
		/// Gets the display name of the memory block.
		/// </summary>
		public string Name => _block.Name;

		/// <summary>
		/// Initializes a new instance of the <see cref="MemoryBlockIdItemViewModel"/> class.
		/// </summary>
		/// <param name="block">The memory block to represent.</param>
		public MemoryBlockIdItemViewModel(MemoryBlock block)
		{
			_block = block;
			Id = block.Id;
			_block.PropertyChanged += Block_PropertyChanged;
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
				_block.PropertyChanged -= Block_PropertyChanged;
		}

		private void Block_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(MemoryBlock.Name))
				RaisePropertyChanged(nameof(Name));
		}
	}

	/// <summary>
	/// ViewModel for the per-agent memory settings tab.
	/// The attached blocks are resolved through the effective (inherited) scope and are
	/// listed as cards; block contents are edited through the
	/// <see cref="EditMemoryBlockDialogViewModel"/> dialog.
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
		/// Gets the shared memory blocks available for attaching to the agent
		/// (blocks that are not attached at the current effective scope).
		/// </summary>
		public RangeObservableCollection<MemoryBlockIdItemViewModel> AvailableBlockIds { get; } = [];

		private MemoryBlockIdItemViewModel? _selectedAvailableBlockId;
		/// <summary>
		/// Gets or sets the memory block selected in the "attach existing" combo box.
		/// </summary>
		public MemoryBlockIdItemViewModel? SelectedAvailableBlockId
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

		/// <summary>
		/// Gets the command that creates a new memory block and attaches it to the agent.
		/// </summary>
		public ICommand AddBlockCommand { get; }

		/// <summary>
		/// Gets the command that attaches the block selected in the combo box to the agent.
		/// </summary>
		public ICommand AttachBlockCommand { get; }

		/// <summary>
		/// Gets the command that opens the edit dialog for the given attached block.
		/// </summary>
		public AsyncRelayCommand<MemoryBlockAttachmentItemViewModel> EditBlockCommand { get; }

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
			EditBlockCommand = new AsyncRelayCommand<MemoryBlockAttachmentItemViewModel>(EditBlockAsync);
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			MemorySettings.PropertyChanged -= MemorySettings_PropertyChanged;
			DisposeItems();
			DisposeAvailableBlocks();
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

		private void DisposeAvailableBlocks()
		{
			foreach (var item in AvailableBlockIds)
				item.Dispose();
		}

		private void UpdateBlocks()
		{
			DisposeItems();
			UpdateAvailableBlocks();

			var items = new List<MemoryBlockAttachmentItemViewModel>(EffectiveBlocks.Count);
			foreach (var attachment in EffectiveBlocks)
				items.Add(new MemoryBlockAttachmentItemViewModel(attachment, EffectiveBlocks, UpdateBlocks));

			BlockItems = items;
		}

		private void UpdateAvailableBlocks()
		{
			DisposeAvailableBlocks();

			var attachedIds = EffectiveBlocks.Select(b => b.Reference.Id).ToHashSet();
			AvailableBlockIds.Reset(BlocksCategory.GetAvailableIds()
				.Where(c => c != SettingsObject.DefaultId && !attachedIds.Contains(c))
				.Select(c => new MemoryBlockIdItemViewModel(BlocksCategory.Get(c)!)));
		}

		private void AddBlock()
		{
			var id = GenerateBlockId();
			var block = BlocksCategory.Get(id);
			block.Name = LocalizationManager.LocalizeStatic("settings-memory_default_block_name");

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
				return;

			EffectiveBlocks.Add(new MemoryBlockAttachment
			{
				Reference = new SettingsReference<MemoryBlock> { Id = id }
			});

			UpdateBlocks();
		}

		private async Task EditBlockAsync(MemoryBlockAttachmentItemViewModel? item)
		{
			if (item?.Block is not { } block)
				return;

			var vm = new EditMemoryBlockDialogViewModel(block, _databaseManager);
			var result = await DialogManager.ShowDialogAsync(vm);

			if (result is not true)
				return;

			if (vm.DuplicatedBlockId is { } duplicatedId)
			{
				Attach(duplicatedId);
			}

			if (vm.RenamedFromId is { } oldId && vm.RenamedToId is { } newId)
			{
				foreach (var attachment in EffectiveBlocks.Where(b => b.Reference.Id == oldId))
					attachment.Reference.Id = newId;
			}

			if (vm.DeletedBlockId is { } deletedId)
			{
				foreach (var attachment in EffectiveBlocks.Where(b => b.Reference.Id == deletedId).ToList())
					EffectiveBlocks.Remove(attachment);
			}

			if (vm.DuplicatedBlockId != null || vm.RenamedToId != null || vm.DeletedBlockId != null)
				UpdateBlocks();
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
