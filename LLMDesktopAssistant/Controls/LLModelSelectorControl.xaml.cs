using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LLMDesktopAssistant.Localization.Resources;
using LLMDesktopAssistant.Modules;
using LLMDesktopAssistant.Modules.Instances;
using RCLargeLanguageModels;
using RCLargeLanguageModels.Clients;

namespace LLMDesktopAssistant.Controls
{
	public class ComboBoxTemplateSelector : DataTemplateSelector
	{
		public DataTemplate AIModelTemplate { get; set; } = null!;
		public DataTemplate FirstTemplate { get; set; } = null!;
		public DataTemplate HeaderTemplate { get; set; } = null!;
		public DataTemplate EmptyTemplate { get; set; } = null!;
		public DataTemplate ErrorTemplate { get; set; } = null!;

		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			if (item is LLModelDescriptorTracked)
				return AIModelTemplate;
			if (item is ComboBoxFirstItemModel)
				return FirstTemplate;
			if (item is ComboBoxHeaderItemModel)
				return HeaderTemplate;
			if (item is ComboBoxEmptyItemModel)
				return EmptyTemplate;
			if (item is ComboBoxErrorItemModel)
				return ErrorTemplate;
			return base.SelectTemplate(item, container);
		}
	}

	public class ComboBoxFirstItemModel
	{
		public string Title { get; set; }

		public ComboBoxFirstItemModel()
		{
			Title = Locale.select_model;
		}
	}

	public class ComboBoxHeaderItemModel
	{
		public string Title { get; set; }

		public ComboBoxHeaderItemModel(string title)
		{
			Title = title;
		}
	}

	public class ComboBoxEmptyItemModel
	{
		public string Title { get; set; }

		public ComboBoxEmptyItemModel()
		{
			Title = Locale.empty_model;
		}
	}

	public class ComboBoxErrorItemModel
	{
		public string Title { get; set; }

		public ComboBoxErrorItemModel(string errorMessage)
		{
			Title = string.Format(Locale.error_model, errorMessage);
		}
	}

	public class ExtendedComboBox : ComboBox
	{
		protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
		{
			base.PrepareContainerForItemOverride(element, item);

			// Туда его блять ( -_•)▄︻テحكـ━一💥
			if (!(item is LLModelDescriptorTracked) && element is ComboBoxItem comboBoxItem)
			{
				comboBoxItem.IsHitTestVisible = false;
				comboBoxItem.Focusable = false;
			}
		}
	}

	public partial class LLModelSelectorControl : UserControl
	{
		public static readonly DependencyProperty SelectedModelProperty =
			DependencyProperty.Register(
				nameof(SelectedModel),
				typeof(LLModelDescriptorTracked),
				typeof(LLModelSelectorControl),
				new PropertyMetadata(LLModelDescriptorTracked.Empty,
					(s, e) => (s as LLModelSelectorControl)?.SelectedModelChanged?.Invoke((LLModelDescriptorTracked)e.NewValue)));

		public static readonly DependencyProperty IsModelValidProperty =
			DependencyProperty.Register(
				nameof(IsModelValid),
				typeof(bool),
				typeof(LLModelSelectorControl),
				new PropertyMetadata((s, e) => (s as LLModelSelectorControl)?.IsModelValidChanged?.Invoke((bool)e.NewValue)));

		public static readonly DependencyProperty IsRefreshButtonVisibleProperty =
			DependencyProperty.Register(
				nameof(IsRefreshButtonVisible),
				typeof(bool),
				typeof(LLModelSelectorControl),
				new PropertyMetadata(true, (s, e) => (s as LLModelSelectorControl)?.IsRefreshButtonVisiblePropertyChanged((bool)e.NewValue)));

		public static readonly DependencyProperty CapabilityFilterProperty =
			DependencyProperty.Register(
				nameof(CapabilityFilter),
				typeof(LLMCapabilities),
				typeof(LLModelSelectorControl),
				new PropertyMetadata(LLMCapabilities.Unknown, (s, e) => (s as LLModelSelectorControl)?.CapabilityFilterPropertyChanged((LLMCapabilities)e.NewValue)));

		public LLModelDescriptorTracked SelectedModel
		{
			get => (LLModelDescriptorTracked)GetValue(SelectedModelProperty);
			set => SetValue(SelectedModelProperty, value);
		}

		public bool IsModelValid
		{
			get => (bool)GetValue(IsModelValidProperty);
			set => SetValue(IsModelValidProperty, value);
		}

		public bool IsRefreshButtonVisible
		{
			get => (bool)GetValue(IsRefreshButtonVisibleProperty);
			set => SetValue(IsRefreshButtonVisibleProperty, value);
		}

		public LLMCapabilities CapabilityFilter
		{
			get => (LLMCapabilities)GetValue(CapabilityFilterProperty);
			set => SetValue(CapabilityFilterProperty, value);
		}

		public event Action<LLModelDescriptorTracked>? SelectedModelChanged;
		public event Action<bool>? IsModelValidChanged;

		public LLModelSelectorControl()
		{
			InitializeComponent();

			var list = ModuleManager.Get<LLModelListModule>();
			list.Registry.RefreshCompleted += Registry_RefreshCompleted;
			Rebuild();
			IsRefreshButtonVisiblePropertyChanged(IsRefreshButtonVisible);

			Selector.Items.Add(new ComboBoxFirstItemModel());
			Selector.SelectionChanged += Selector_SelectionChanged;
			Selector.DropDownOpened += Selector_DropDownOpened;
			SelectedModelChanged += Private_SelectedModelChanged;
		}

		private void IsRefreshButtonVisiblePropertyChanged(bool newValue)
		{
			RefreshButton.Visibility = newValue ? Visibility.Visible : Visibility.Collapsed;
		}

		private void CapabilityFilterPropertyChanged(LLMCapabilities newValue)
		{
			Rebuild();
		}

		private bool refreshFlag = false;

		private void Selector_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (refreshFlag)
				return;

			if (Selector.SelectedItem is LLModelDescriptorTracked model)
				SelectedModel = model;
			else
				SelectedModel = LLModelDescriptorTracked.Empty;
		}

		private void Private_SelectedModelChanged(LLModelDescriptorTracked model)
		{
			IsModelValid = model != null;

			if (model != null)
			{
				if (!Selector.Items.Contains(model))
					Selector.Items.Insert(1, model);
				Selector.SelectedItem = model;
			}
			else
				Selector.SelectedIndex = 0;
		}

		private void Registry_RefreshCompleted(object? sender, EventArgs e)
		{
			Dispatcher.Invoke(() =>
			{
				Rebuild();
			});
		}

		private void Selector_DropDownOpened(object? sender, EventArgs e)
		{
			// Task.Run(async () => await RefreshAsync());
		}

		public async Task RefreshAsync()
		{
			var list = ModuleManager.Get<LLModelListModule>();
			await list.Registry.RefreshModelsAsync();
		}

		private void RefreshButton_Click(object sender, RoutedEventArgs e)
		{
			Task.Run(() => RefreshAsync());
		}

		private ImmutableHashSet<LLModelDescriptor> _prevModels = [];

		private void Rebuild()
		{
			var list = ModuleManager.Get<LLModelListModule>();
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
			Selector.Items.Add(new ComboBoxFirstItemModel());
			refreshFlag = false;

			// Find the previous model
			bool found = false;
			foreach (var clientTuple in grouped)
				foreach (var model in clientTuple.Value)
					if (model == prevSelected)
						found = true;

			if (!found && prevSelected != null && prevSelected != LLModelDescriptorTracked.Empty)
			{
				Selector.Items.Add(prevSelected);
				Selector.SelectedItem = prevSelected;
			}

			refreshFlag = true;
			foreach (var clientTuple in grouped)
			{
				Selector.Items.Add(new ComboBoxHeaderItemModel(clientTuple.Key.DisplayName));

				int count = 0;

				foreach (var model in clientTuple.Value)
				{
					Selector.Items.Add(model);
					if (model == prevSelected)
						Selector.SelectedItem = prevSelected;
					count++;
				}

				if (count == 0)
					Selector.Items.Add(new ComboBoxEmptyItemModel());
			}
			refreshFlag = false;
		}
	}
}
