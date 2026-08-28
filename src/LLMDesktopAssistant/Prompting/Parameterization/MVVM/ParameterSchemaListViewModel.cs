using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.MVVM;
using LLMDesktopAssistant.Prompting.Parameterization.Elements;
using LLMDesktopAssistant.Prompting.Parameterization.Values;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Prompting.Parameterization.MVVM
{
	/// <summary>
	/// ViewModel for rendering a list parameter schema element and its values.
	/// Supports adding and removing items within the [Min, Max] range.
	/// </summary>
	[ViewModelFor(typeof(ParameterSchemaListView))]
	public class ParameterSchemaListViewModel : ViewModelBase
	{
		private readonly ParameterListElement _element;
		private readonly ParameterSchemaArrayValue _value;
		private readonly AppendOnlyList<ParameterValidationLogEntry> _log = [];

		public RangeObservableCollection<ParameterSchemaListItemViewModel> Items { get; } = [];

		public IRelayCommand AddCommand { get; }

		public IRelayCommand<ParameterSchemaListItemViewModel> RemoveCommand { get; }

		public bool CanAdd => Items.Count < _element.Max;

		public bool CanRemove => Items.Count > _element.Min;

		public ParameterSchemaListViewModel(ParameterListElement element, ParameterSchemaArrayValue value)
		{
			_element = element;
			_value = value;

			foreach (var itemValue in value.Items)
				Items.Add(new ParameterSchemaListItemViewModel(element.ItemsSchema, itemValue));

			AddCommand = new RelayCommand(AddItem, () => CanAdd);
			RemoveCommand = new RelayCommand<ParameterSchemaListItemViewModel>(RemoveItem, _ => CanRemove);
		}

		private void AddItem()
		{
			if (!CanAdd)
				return;
			var itemValue = _element.ItemsSchema.CreateOrFixValue(null, _log);
			_value.Items.Add(itemValue);
			Items.Add(new ParameterSchemaListItemViewModel(_element.ItemsSchema, itemValue));
			RefreshCommands();
		}

		private void RemoveItem(ParameterSchemaListItemViewModel? item)
		{
			if (item is null || !CanRemove)
				return;
			_value.Items.Remove(item.Value);
			Items.Remove(item);
			RefreshCommands();
		}

		private void RefreshCommands()
		{
			AddCommand.NotifyCanExecuteChanged();
			RemoveCommand.NotifyCanExecuteChanged();
		}
	}

	/// <summary>
	/// A single item of a list parameter schema element.
	/// </summary>
	public class ParameterSchemaListItemViewModel
	{
		public ParameterSchemaElement Element { get; }

		public ParameterSchemaValue Value { get; }

		public Control Control { get; }

		public ParameterSchemaListItemViewModel(ParameterSchemaElement element, ParameterSchemaValue value)
		{
			Element = element;
			Value = value;
			Control = element.CreateControl(value);
		}
	}
}
