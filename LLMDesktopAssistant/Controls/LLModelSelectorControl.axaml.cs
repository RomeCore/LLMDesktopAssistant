using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Localization.Resources;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Services.Instances;
using RCLargeLanguageModels;
using RCLargeLanguageModels.Clients;
using Serilog;

namespace LLMDesktopAssistant.Controls
{
	public class LLModelWrapperItemModel
	{
		public static LLModelWrapperItemModel Empty { get; } = new LLModelWrapperItemModel { Model = LLModelDescriptorTracked.Empty };

		public required LLModelDescriptorTracked Model { get; init; }
		public bool IsEmpty => LLModelDescriptorTracked.Empty == Model;
		public string FallbackName => Model.FullName.ToNullIfEmpty() ?? LocalizationManager.LocalizeStatic("empty_model");
	}

	public class ComboBoxHeaderItemModel
	{
		public required string Title { get; init; }
	}

	public class ComboBoxEmptyItemModel
	{
		public string Title { get; init; } = LocalizationManager.LocalizeStatic("empty_model");
	}

	public class ComboBoxErrorItemModel
	{
		public string Title => LocalizationManager.LocalizeStaticFormat("empty_model", ErrorMessage);
		public required string ErrorMessage { get; init; }
	}

	public class LLModelSelectorComboBox : ComboBox
	{
		protected override Type StyleKeyOverride => typeof(ComboBox);

		protected override void PrepareContainerForItemOverride(Control element, object? item, int index)
		{
			base.PrepareContainerForItemOverride(element, item, index);

			// Туда его блять ( -_•)▄︻テحكـ━一💥
			if (element is ComboBoxItem comboBoxItem)
			{
				if (item is not LLModelWrapperItemModel)
				{
					comboBoxItem.IsHitTestVisible = false;
					comboBoxItem.Focusable = false;
				}
				else
				{
					// Так как ComboBox использует ебучую виртуализацию (переиспользует старые ComboBoxItem для новых элементов),
					// то возвращаем состояние элемента обратно
					comboBoxItem.IsHitTestVisible = true;
					comboBoxItem.Focusable = true;
				}
			}
		}
	}

	public partial class LLModelSelectorControl : UserControl
	{
		public static readonly StyledProperty<LLModelDescriptorTracked> SelectedModelProperty =
			AvaloniaProperty.Register<LLModelSelectorControl, LLModelDescriptorTracked>(
				nameof(SelectedModel));

		public static readonly StyledProperty<bool> IsModelValidProperty =
			AvaloniaProperty.Register<LLModelSelectorControl, bool>(
				nameof(IsModelValid));

		public static readonly StyledProperty<bool> IsRefreshButtonVisibleProperty =
			AvaloniaProperty.Register<LLModelSelectorControl, bool>(
				nameof(IsRefreshButtonVisible),
				true);

		public static readonly StyledProperty<LLMCapabilities> CapabilityFilterProperty =
			AvaloniaProperty.Register<LLModelSelectorControl, LLMCapabilities>(
				nameof(CapabilityFilter),
				LLMCapabilities.Unknown);

		public LLModelDescriptorTracked SelectedModel
		{
			get => GetValue(SelectedModelProperty);
			set => SetValue(SelectedModelProperty, value);
		}

		public bool IsModelValid
		{
			get => GetValue(IsModelValidProperty);
			set => SetValue(IsModelValidProperty, value);
		}

		public bool IsRefreshButtonVisible
		{
			get => GetValue(IsRefreshButtonVisibleProperty);
			set => SetValue(IsRefreshButtonVisibleProperty, value);
		}

		public LLMCapabilities CapabilityFilter
		{
			get => GetValue(CapabilityFilterProperty);
			set => SetValue(CapabilityFilterProperty, value);
		}

		public event Action<LLModelDescriptorTracked>? SelectedModelChanged;
		public event Action<bool>? IsModelValidChanged;

		static LLModelSelectorControl()
		{
			SelectedModelProperty.Changed.AddClassHandler<LLModelSelectorControl>((o, e) => o.SelectedModelChanged?.Invoke((LLModelDescriptorTracked)e.NewValue!));
			IsModelValidProperty.Changed.AddClassHandler<LLModelSelectorControl>((o, e) => o.IsModelValidChanged?.Invoke((bool)e.NewValue!));
			IsRefreshButtonVisibleProperty.Changed.AddClassHandler<LLModelSelectorControl>((o, e) => o.IsRefreshButtonVisiblePropertyChanged((bool)e.NewValue!));
			CapabilityFilterProperty.Changed.AddClassHandler<LLModelSelectorControl>((o, e) => o.CapabilityFilterPropertyChanged((LLMCapabilities)e.NewValue!));
		}

		public LLModelSelectorControl()
		{
			InitializeComponent();

			// Из-за этой ебучей подписки у меня ебейшие утечки
			// Не удаляетя UserInput -> MessageSequence (ChatMessage+++), и настройки заодно тоже не удаляются
			var list = ServiceRegistry.Provider.GetRequiredService<LLModelListService>();
			var weakRef = new WeakReference(this);

			void ListRefreshBegan(object? s, EventArgs e)
			{
				var target = weakRef.Target as LLModelSelectorControl;
				if (target == null)
					list.Registry.RefreshBegan -= ListRefreshBegan;
				else
					target.Registry_RefreshBegan(s, e);
			}
			void ListRefreshCompleted(object? s, EventArgs e)
			{
				var target = weakRef.Target as LLModelSelectorControl;
				if (target == null)
					list.Registry.RefreshCompleted -= ListRefreshCompleted;
				else
					target.Registry_RefreshCompleted(s, e);
			}
			list.Registry.RefreshCompleted += ListRefreshCompleted;

			Selector.SelectionChanged += Selector_SelectionChanged;
			Selector.DropDownOpened += Selector_DropDownOpened;
			SelectedModelChanged += Private_SelectedModelChanged;

			Rebuild();
			IsRefreshButtonVisiblePropertyChanged(IsRefreshButtonVisible);
		}

		private void IsRefreshButtonVisiblePropertyChanged(bool newValue)
		{
			RefreshButton.IsVisible = newValue;
		}

		private void CapabilityFilterPropertyChanged(LLMCapabilities newValue)
		{
			Rebuild();
		}

		private bool refreshFlag = false;

		private void Selector_SelectionChanged(object? sender, SelectionChangedEventArgs e)
		{
			if (refreshFlag)
				return;

			if (Selector.SelectedItem is LLModelWrapperItemModel modelWrapper)
				SelectedModel = modelWrapper.Model;
			else
				SelectedModel = LLModelDescriptorTracked.Empty;
		}

		private void Private_SelectedModelChanged(LLModelDescriptorTracked model)
		{
			IsModelValid = model != null;

			if (model != null)
			{
				var existingItem = Selector.Items.FirstOrDefault(m =>
					m is LLModelWrapperItemModel modelWrapper && modelWrapper.Model == model);
				if (existingItem == null)
				{
					existingItem = new LLModelWrapperItemModel { Model = model };
					Selector.Items.Insert(0, existingItem);
				}
				Selector.SelectedItem = existingItem;
			}
			else
				Selector.SelectedIndex = 0;
		}

		private void Registry_RefreshBegan(object? sender, EventArgs e)
		{
			Dispatcher.Invoke(() =>
			{
				SelectorRoot.IsEnabled = false;
			});
		}

		private void Registry_RefreshCompleted(object? sender, EventArgs e)
		{
			Dispatcher.Invoke(() =>
			{
				SelectorRoot.IsEnabled = true;
				Rebuild();
			});
		}

		private void Selector_DropDownOpened(object? sender, EventArgs e)
		{
			// Task.Run(async () => await RefreshAsync());
		}

		public async Task RefreshAsync()
		{
			var list = ServiceRegistry.Provider.GetRequiredService<LLModelListService>();
			await list.Registry.RefreshModelsAsync();
		}

		private void RefreshButton_Click(object? sender, RoutedEventArgs e)
		{
			Task.Run(() => RefreshAsync());
		}

		private ImmutableHashSet<LLModelDescriptor> _prevModels = [];

		private void Rebuild()
		{
			var list = ServiceRegistry.Provider.GetRequiredService<LLModelListService>();
			var registry = list.Registry;
			var models = CapabilityFilter == LLMCapabilities.Unknown
				? registry.Models
				: registry.Models.FilterByCapabilities(CapabilityFilter);

			if (models.ToHashSet().SetEquals(_prevModels))
				return;

			var grouped = models.GroupBy(m => m.Client)
				.OrderBy(g => g.Key.DisplayName)
				.ToMultiValueDictionary(
					m => m.Key,
					m => (IEnumerable<LLModelDescriptorTracked>)m.OrderBy(_m => _m.DisplayName)
						.Select(m => registry.GetModel(m.FullName)).ToList());

			var prevSelected = SelectedModel;

			refreshFlag = true;
			Selector.Items.Clear();
			Selector.Items.Add(LLModelWrapperItemModel.Empty);
			refreshFlag = false;

			// Find the previous model
			bool found = false;
			foreach (var clientTuple in grouped)
				foreach (var model in clientTuple.Value)
					if (model == prevSelected)
						found = true;

			if (!found && prevSelected != null && prevSelected != LLModelDescriptorTracked.Empty)
			{
				var prevSelectedItem = new LLModelWrapperItemModel { Model = prevSelected };
				Selector.Items.Add(prevSelectedItem);
				Selector.SelectedItem = prevSelectedItem;
			}

			refreshFlag = true;
			foreach (var clientTuple in grouped)
			{
				Selector.Items.Add(new ComboBoxHeaderItemModel { Title = clientTuple.Key.DisplayName });

				int count = 0;

				foreach (var model in clientTuple.Value)
				{
					var modelWrapper = new LLModelWrapperItemModel { Model = model };
					Selector.Items.Add(modelWrapper);
					if (model == prevSelected)
						Selector.SelectedItem = modelWrapper;
					count++;
				}

				if (count == 0)
					Selector.Items.Add(new ComboBoxEmptyItemModel());
			}
			refreshFlag = false;
		}
	}
}
