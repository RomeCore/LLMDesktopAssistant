using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace LLMDesktopAssistant.Addons.MVVM;

public partial class AddonCardView : UserControl
{
	public AddonCardView()
	{
		InitializeComponent();
	}

	/// <summary>
	/// Toggles the children of a group card when its header is clicked on free space: clicks that belong
	/// to an interactive control of the header (a toggle, a selector, an action button) are left to them.
	/// </summary>
	private void Header_PointerPressed(object? sender, PointerPressedEventArgs e)
	{
		if (DataContext is not AddonCardViewModel card || !card.HasChildren)
			return;

		if (e.Source is Visual source && HasInteractiveAncestor(source))
			return;

		card.IsChildrenExpanded = !card.IsChildrenExpanded;
		e.Handled = true;
	}

	private static bool HasInteractiveAncestor(Visual source)
	{
		foreach (var current in source.GetSelfAndVisualAncestors())
		{
			if (current is Button or ToggleButton or ToggleSwitch or ComboBox or TextBox)
				return true;

			if (current is AddonCardView)
				return false;
		}

		return false;
	}
}
