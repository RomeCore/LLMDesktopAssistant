using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.StructuredValues.Parameterization.Controls;
using LLMDesktopAssistant.StructuredValues.Parameterization.Elements;
using LLMDesktopAssistant.StructuredValues.Reactive;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.StructuredValues.Parameterization.MVVM
{
	/// <summary>
	/// ViewModel for rendering a list parameter schema element and its values.
	/// Supports adding and removing items within the [Min, Max] range.
	/// </summary>
	[ViewModelFor(typeof(ParameterSchemaListView))]
	public class ParameterSchemaListViewModel : ViewModelBase
	{
		private readonly ParameterListElement _element;
		private readonly ReactiveNodeArrayValue _value;
		private readonly IParameterSchemaControlFactoryManager _controlFactory;
		private readonly AppendOnlyList<ParameterValidationLogEntry> _log = [];

		public RangeObservableCollection<ParameterSchemaListItemViewModel> Items { get; } = [];

		public IRelayCommand AddCommand { get; }

		public IRelayCommand<ParameterSchemaListItemViewModel> RemoveCommand { get; }

		public bool CanAdd => Items.Count < _element.Max;

		public bool CanRemove => Items.Count > _element.Min;

		public ParameterSchemaListViewModel(ParameterListElement element, ReactiveNodeArrayValue value,
			IParameterSchemaControlFactoryManager controlFactory)
		{
			_element = element;
			_value = value;
			_controlFactory = controlFactory;

			foreach (var itemValue in value.Items)
				Items.Add(CreateItem(itemValue));

			AddCommand = new RelayCommand(AddItem, () => CanAdd);
			RemoveCommand = new RelayCommand<ParameterSchemaListItemViewModel>(RemoveItem, _ => CanRemove);
		}

		private ParameterSchemaListItemViewModel CreateItem(ReactiveNodeValue itemValue)
		{
			return new ParameterSchemaListItemViewModel(_element.ItemsSchema, itemValue,
				_controlFactory.CreateControl(_element.ItemsSchema, itemValue));
		}

		private void AddItem()
		{
			if (!CanAdd)
				return;
			var itemValue = _element.ItemsSchema.CreateOrFixValue(null, _log);
			_value.Items.Add(itemValue);
			Items.Add(CreateItem(itemValue));
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

		public ReactiveNodeValue Value { get; }

		public Control Control { get; }

		public ParameterSchemaListItemViewModel(ParameterSchemaElement element, ReactiveNodeValue value, Control control)
		{
			Element = element;
			Value = value;
			Control = control;
		}
	}
}
