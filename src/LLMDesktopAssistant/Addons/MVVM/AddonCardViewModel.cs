using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Localization;
using Material.Icons;

namespace LLMDesktopAssistant.Addons.MVVM
{
	[ViewModelFor(typeof(AddonCardView))]
	public class AddonCardViewModel : ViewModelBase
	{
		public ImmutableList<IAddonCardElement> Elements { get; }

		public MaterialIconKind Icon { get; }
		public LocaleKeyBase Name { get; }
		public LocaleKeyBase Description { get; }

		public ImmutableList<IAddonCardChange> Changes { get; }
		public ImmutableList<IAddonCardChange> LeftChanges { get; }
		public ImmutableList<IAddonCardChange> RightChanges { get; }
		public ImmutableList<IAddonCardChip> Chips { get; }
		public ImmutableList<IAddonCardBlock> Blocks { get; }
		public ImmutableList<IAddonCardActionRowElement> ActionRowElements { get; }
		public ImmutableList<IAddonCardAction> Actions { get; }
		public ImmutableList<IAddonCardBlock> DetailBlocks { get; }

		public ICommand ResetCommand { get; }

		public bool IsDetailsVisible
		{
			get;
			set => SetProperty(ref field, value);
		}

		public AddonCardViewModel(MaterialIconKind icon, LocaleKeyBase name,
			LocaleKeyBase description, IEnumerable<IAddonCardElement> elements)
		{
			Icon = icon;
			Name = name;
			Description = description;

			Elements = [.. elements];

			Changes = [.. Elements.OfType<IAddonCardChange>().OrderBy(e => e.Order)];
			LeftChanges = [.. Changes.Where(c => c.IsShownLeft)];
			RightChanges = [.. Changes.Where(c => !c.IsShownLeft)];
			Chips = [.. Elements.OfType<IAddonCardChip>().OrderBy(e => e.Order)];
			Blocks = [.. Elements.OfType<IAddonCardBlock>().Where(b => !b.IsDetail).OrderBy(e => e.Order)];
			ActionRowElements = [.. Elements.OfType<IAddonCardActionRowElement>().OrderBy(e => e.Order)];
			Actions = [.. Elements.OfType<IAddonCardAction>().OrderBy(e => e.Order)];
			DetailBlocks = [.. Elements.OfType<IAddonCardBlock>().Where(b => b.IsDetail).OrderBy(e => e.Order)];

			ResetCommand = new RelayCommand(Reset);
		}

		private void Reset()
		{
			foreach (var change in Changes)
				change.Reset();
		}
	}
}
