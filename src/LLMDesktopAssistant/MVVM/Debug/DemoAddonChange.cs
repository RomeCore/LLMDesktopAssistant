using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Addons.MVVM;

namespace LLMDesktopAssistant.MVVM.Debug;

/// <summary>
/// A fully interactive <see cref="IAddonCardHeaderChange"/> used by the addon cards debug page.
/// It holds a mutable value, reports itself as changed while the value differs from the definition
/// value (so the card draws the accent marker) and resets itself back to that value.
/// </summary>
public class DemoAddonChange : ViewModelBase, IAddonCardHeaderChange
{
	private readonly object? _definitionValue;
	private object? _value;

	/// <summary>
	/// Initializes a new instance of the <see cref="DemoAddonChange"/> class.
	/// </summary>
	/// <param name="definitionValue">The value taken from the addon definition.</param>
	/// <param name="isShownLeft">Whether the slot is placed to the left of the addon name.</param>
	/// <param name="order">The order of the element inside its group.</param>
	public DemoAddonChange(object? definitionValue, bool isShownLeft = true, int order = 0)
	{
		_definitionValue = definitionValue;
		_value = definitionValue;
		IsShownLeft = isShownLeft;
		Order = order;
	}

	/// <inheritdoc/>
	public object? Content { get; private set; }

	/// <inheritdoc/>
	public bool IsShownLeft { get; }

	/// <inheritdoc/>
	public int Order { get; }

	/// <inheritdoc/>
	public bool IsChanged => !Equals(_value, _definitionValue);

	/// <summary>
	/// Gets or sets the current (override) value. Setting a value different from the definition
	/// value turns the change into the "changed" state.
	/// </summary>
	public object? Value
	{
		get => _value;
		set
		{
			if (SetProperty(ref _value, value))
				RaisePropertyChanged(nameof(IsChanged));
		}
	}

	public ICommand? ResetCommand => field ??= new RelayCommand(Reset);

	public void Reset() => Value = _definitionValue;

	/// <summary>
	/// Creates a change with a checkbox in its header slot.
	/// </summary>
	/// <param name="definitionValue">The value taken from the addon definition.</param>
	/// <param name="isShownLeft">Whether the slot is placed to the left of the addon name.</param>
	/// <param name="order">The order of the element inside its group.</param>
	public static DemoAddonChange Toggle(bool definitionValue, bool isShownLeft = true, int order = 0)
	{
		var change = new DemoAddonChange(definitionValue, isShownLeft, order);

		var box = new CheckBox
		{
			DataContext = change,
			VerticalAlignment = VerticalAlignment.Center
		};
		box.Bind(CheckBox.IsCheckedProperty, new Binding(nameof(Value)) { Mode = BindingMode.TwoWay });

		change.Content = box;
		return change;
	}

	/// <summary>
	/// Creates a change with a combobox in its header slot.
	/// </summary>
	/// <param name="options">The list of the available options.</param>
	/// <param name="definitionValue">The value taken from the addon definition.</param>
	/// <param name="isShownLeft">Whether the slot is placed to the left of the addon name.</param>
	/// <param name="order">The order of the element inside its group.</param>
	public static DemoAddonChange Selector(IReadOnlyList<string> options, string definitionValue,
		bool isShownLeft = true, int order = 0)
	{
		var change = new DemoAddonChange(definitionValue, isShownLeft, order);

		var combo = new ComboBox
		{
			DataContext = change,
			ItemsSource = options,
			MinWidth = 110,
			FontSize = 12,
			VerticalAlignment = VerticalAlignment.Center
		};
		combo.Bind(ComboBox.SelectedItemProperty, new Binding(nameof(Value)) { Mode = BindingMode.TwoWay });

		change.Content = combo;
		return change;
	}
}
