using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using LLMDesktopAssistant.Addons.MVVM;
using LLMDesktopAssistant.Converters;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.LLM.MVVM.Settings;
using LLMDesktopAssistant.LLM.MVVM.Settings.Agents;
using Material.Icons;
using Material.Icons.Avalonia;

namespace LLMDesktopAssistant.Tools.MVVM.Elements
{
	/// <summary>
	/// The details block of a tool card that edits the policy mask of the tool: one three-state toggle per
	/// behaviour flag declared by the tool. The block is active only while the effective approval level of
	/// the tool is policy-based.
	/// </summary>
	public class AddonCardToolPolicyMaskBlock : AddonCardBlockChange, ISetPolicyMaskFlag
	{
		private readonly AddonCardContext<ToolInfo, ToolChange> _context;
		private readonly ImmutableList<ToolBehaviourMaskItem> _items;

		private ToolChange? _subscribedChange;

		/// <summary>
		/// Initializes a new instance of the <see cref="AddonCardToolPolicyMaskBlock"/> class.
		/// </summary>
		/// <param name="context">The addon, its change set and the page that owns the card.</param>
		public AddonCardToolPolicyMaskBlock(AddonCardContext<ToolInfo, ToolChange> context)
		{
			ArgumentNullException.ThrowIfNull(context);

			_context = context;

			Visibility = AddonCardBlockVisibility.Details;
			Title = Locale.GetKey("card.tools.policy_mask");

			var mask = EffectiveMask;
			_items = [.. ToolBehaviourFlagInfo.CreateForFlags(context.Addon.DefaultExpectedBehaviour)
				.Select(info => new ToolBehaviourMaskItem(this, info, GetMaskState(mask, info.Flag), false))];

			var panel = new WrapPanel
			{
				DataContext = this,
				ItemSpacing = 8,
				LineSpacing = 8
			};
			panel.Bind(InputElement.IsEnabledProperty, new Binding(nameof(IsPolicyBased)));

			foreach (var item in _items)
				panel.Children.Add(CreateToggle(item));

			Content = panel;

			_context.PropertyChanged += Context_PropertyChanged;
			SubscribeChange();

			if (Toolset is not null)
				Toolset.PropertyChanged += Toolset_PropertyChanged;

			SyncIsChanged();
		}

		/// <summary>
		/// Gets a value indicating whether the policy mask is applied by the current approval level
		/// of the tool.
		/// </summary>
		public bool IsPolicyBased => EffectiveApprovalLevel.IsPolicyBased();

		private ToolsetConfiguration? Toolset => _context.SetConfig as ToolsetConfiguration;

		private ToolApprovalLevel EffectiveApprovalLevel =>
			_context.Change?.ApprovalLevel
			?? _context.Addon.ApprovalLevel
			?? Toolset?.DefaultApprovalLevel
			?? ToolApprovalLevel.PolicyBased;

		private ToolPolicyMask EffectiveMask => _context.Change?.PolicyMask ?? _context.Addon.PolicyMask ?? default;

		/// <inheritdoc/>
		public void SetPolicyMaskFlag(ToolBehaviour flag, bool? state)
		{
			var mask = EffectiveMask;
			mask = state switch
			{
				true => new ToolPolicyMask
				{
					AutoApproveBehaviours = mask.AutoApproveBehaviours | flag,
					DisallowedBehaviours = mask.DisallowedBehaviours & ~flag
				},
				false => new ToolPolicyMask
				{
					AutoApproveBehaviours = mask.AutoApproveBehaviours & ~flag,
					DisallowedBehaviours = mask.DisallowedBehaviours | flag
				},
				_ => new ToolPolicyMask
				{
					AutoApproveBehaviours = mask.AutoApproveBehaviours & ~flag,
					DisallowedBehaviours = mask.DisallowedBehaviours & ~flag
				}
			};

			_context.EnsureChange().PolicyMask = mask;
			RefreshItems();
			SyncIsChanged();
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
			{
				_context.PropertyChanged -= Context_PropertyChanged;

				if (_subscribedChange is not null)
					_subscribedChange.PropertyChanged -= Change_PropertyChanged;

				if (Toolset is not null)
					Toolset.PropertyChanged -= Toolset_PropertyChanged;
			}
		}

		/// <inheritdoc/>
		protected override void ResetCore()
		{
			base.ResetCore();

			if (_context.Change is { } change)
				change.PolicyMask = null;

			RefreshItems();
			SyncIsChanged();
		}

		private void SubscribeChange()
		{
			if (ReferenceEquals(_subscribedChange, _context.Change))
				return;

			if (_subscribedChange is not null)
				_subscribedChange.PropertyChanged -= Change_PropertyChanged;

			_subscribedChange = _context.Change;

			if (_subscribedChange is not null)
				_subscribedChange.PropertyChanged += Change_PropertyChanged;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is not nameof(AddonCardContext<,>.Change))
				return;

			SubscribeChange();
			RaisePropertyChanged(nameof(IsPolicyBased));
			RefreshItems();
			SyncIsChanged();
		}

		private void Change_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(ToolChange.ApprovalLevel):
					RaisePropertyChanged(nameof(IsPolicyBased));
					break;

				case nameof(ToolChange.PolicyMask):
					RefreshItems();
					SyncIsChanged();
					break;
			}
		}

		private void Toolset_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(ToolsetConfiguration.DefaultApprovalLevel))
				RaisePropertyChanged(nameof(IsPolicyBased));
		}

		private void RefreshItems()
		{
			var mask = EffectiveMask;

			foreach (var item in _items)
				item.Refresh(GetMaskState(mask, item.Flag));
		}

		private void SyncIsChanged()
		{
			IsChanged = _context.Change?.PolicyMask is not null;
		}

		private static bool? GetMaskState(ToolPolicyMask mask, ToolBehaviour flag)
		{
			if (mask.AutoApproveBehaviours.HasFlag(flag))
				return true;
			if (mask.DisallowedBehaviours.HasFlag(flag))
				return false;
			return null;
		}

		private static ToggleButton CreateToggle(ToolBehaviourMaskItem item)
		{
			var icon = new MaterialIcon
			{
				Kind = item.Icon,
				Width = 24,
				Height = 24,
				Foreground = item.Color
			};

			var toggle = new ToggleButton
			{
				IsThreeState = true,
				Width = 32,
				Height = 32,
				Padding = new Thickness(0),
				CornerRadius = new CornerRadius(4),
				Background = Brushes.Transparent,
				BorderThickness = new Thickness(0),
				Content = icon,
			};
			toggle.Classes.Add("groupStateToggle");

			toggle.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(ToolBehaviourMaskItem.IsChecked))
			{
				Mode = BindingMode.TwoWay,
				Source = item
			});
			toggle.Bind(InputElement.IsEnabledProperty, new Binding(nameof(ToolBehaviourMaskItem.IsNone))
			{
				Converter = InverseBooleanConverter.Instance,
				Source = item
			});

			ToolTip.SetTip(toggle, new StackPanel
			{
				Spacing = 4,
				Children =
				{
					CreateToolTipLine(item, $"{nameof(ToolBehaviourMaskItem.DisplayName)}.{nameof(LocaleKeyBase.Value)}", bold: true),
					CreateToolTipLine(item, $"{nameof(ToolBehaviourMaskItem.Description)}.{nameof(LocaleKeyBase.Value)}"),
					CreateToolTipLine(item, $"{nameof(ToolBehaviourMaskItem.StateName)}.{nameof(LocaleKeyBase.Value)}",
						brushPath: nameof(ToolBehaviourMaskItem.StateColor))
				}
			});

			return toggle;
		}

		private static TextBlock CreateToolTipLine(object source, string path, bool bold = false, string? brushPath = null)
		{
			var text = new TextBlock { TextWrapping = TextWrapping.Wrap };

			if (bold)
				text.FontWeight = FontWeight.SemiBold;

			text.Bind(TextBlock.TextProperty, new Binding(path) { Source = source });

			if (brushPath is not null)
				text.Bind(TextBlock.ForegroundProperty, new Binding(brushPath) { Source = source });

			return text;
		}
	}
}
