using Avalonia.Media;
using LLMDesktopAssistant.LLM.Messages;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Tools;
using Material.Icons;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.Agents;

/// <summary>
/// Represents a per-tool policy mask toggle for a single <see cref="ToolBehaviour"/> flag.
/// The toggle cycles through three states: default, auto-approve and disallowed.
/// </summary>
public class ToolBehaviourMaskItem : NotifyPropertyChanged
{
	private readonly ToolItemViewModel _owner;
	private bool? _isChecked;

	/// <summary>
	/// Value indicating whether the <see cref="Flag"/> is <see cref="ToolBehaviour.None"/>.
	/// </summary>
	public bool IsNone { get; }

	/// <summary>
	/// The <see cref="ToolBehaviour"/> flag this item represents.
	/// </summary>
	public ToolBehaviour Flag { get; }

	/// <summary>
	/// Gets the icon associated with this behaviour flag.
	/// </summary>
	public MaterialIconKind Icon { get; }

	/// <summary>
	/// Localized display name of the behaviour.
	/// </summary>
	public string DisplayName { get; }

	/// <summary>
	/// Localized description of the behaviour.
	/// </summary>
	public string Description { get; }

	/// <summary>
	/// Gets the localized name of the current override state.
	/// </summary>
	public string StateName { get; private set; } = string.Empty;

	/// <summary>
	/// Gets the color of the current behaviour flag.
	/// </summary>
	public IBrush Color { get; }

	/// <summary>
	/// Gets the color of the current override state: gray for default, green for auto-approve, red for disallowed.
	/// </summary>
	public IBrush StateColor { get; private set; } = Brushes.Gray;

	/// <summary>
	/// Gets or sets the current override state: <see langword="null"/> - default,
	/// <see langword="true"/> - auto-approve, <see langword="false"/> - disallowed.
	/// </summary>
	public bool? IsChecked
	{
		get => _isChecked;
		set
		{
			if (IsNone || _isChecked == value)
				return;
			_isChecked = value;
			_owner.SetPolicyMaskFlag(Flag, value);
			RaisePropertyChanged();
			UpdateVisuals();
		}
	}

	public ToolBehaviourMaskItem(ToolItemViewModel owner, ToolBehaviourFlagInfo flagInfo, bool? isChecked)
	{
		_owner = owner;
		IsNone = flagInfo.Flag == ToolBehaviour.None;
		Flag = flagInfo.Flag;
		_isChecked = IsNone ? null : isChecked;

		Icon = flagInfo.Icon;
		DisplayName = flagInfo.DisplayName;
		Description = flagInfo.Description;
		Color = flagInfo.Color;
		UpdateVisuals();
	}

	/// <summary>
	/// Resynchronizes the toggle state from the effective policy mask.
	/// </summary>
	/// <param name="isChecked">The new override state.</param>
	public void Refresh(bool? isChecked)
	{
		if (_isChecked == isChecked)
			return;
		_isChecked = isChecked;
		RaisePropertyChanged(nameof(IsChecked));
		UpdateVisuals();
	}

	private void UpdateVisuals()
	{
		StateName = _isChecked switch
		{
			true => LocalizationManager.LocalizeStatic("settings.tool.behaviour_policy.auto_approve"),
			false => LocalizationManager.LocalizeStatic("settings.tool.behaviour_policy.disallowed"),
			_ => LocalizationManager.LocalizeStatic("settings.tool.behaviour_policy.default")
		};
		StateColor = _isChecked switch
		{
			true => Brushes.Green,
			false => Brushes.Red,
			_ => Brushes.Gray
		};
		RaisePropertyChanged(nameof(StateName));
		RaisePropertyChanged(nameof(StateColor));
	}
}
