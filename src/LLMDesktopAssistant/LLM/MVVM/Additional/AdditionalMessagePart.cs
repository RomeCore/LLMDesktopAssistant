using Avalonia.Media;
using Material.Icons;

namespace LLMDesktopAssistant.LLM.MVVM.Additional
{
	/// <summary>
	/// The base class for message parts - small additional view models that are rendered
	/// as compact chips in a WrapPanel at the bottom of a message or a tool call.
	/// </summary>
	public abstract class AdditionalMessagePart : AdditionalMessageViewModel
	{
		private MaterialIconKind? _badgeIcon;
		/// <summary>
		/// Gets or sets the icon of the badge shown on the part chip.
		/// </summary>
		public MaterialIconKind? BadgeIcon
		{
			get => _badgeIcon;
			set => SetProperty(ref _badgeIcon, value);
		}

		private string? _badgeTitle;
		/// <summary>
		/// Gets or sets the title of the badge shown on the part chip.
		/// </summary>
		public string? BadgeTitle
		{
			get => _badgeTitle;
			set => SetProperty(ref _badgeTitle, value);
		}

		private Color? _badgeColor;
		/// <summary>
		/// Gets or sets the color of the badge shown on the part chip.
		/// </summary>
		public Color? BadgeColor
		{
			get => _badgeColor;
			set => SetProperty(ref _badgeColor, value);
		}

		/// <summary>
		/// Creates a shallow copy of this part. Used when editing messages to avoid
		/// sharing part instances between the draft state and the original message.
		/// </summary>
		public virtual AdditionalMessagePart Clone() => (AdditionalMessagePart)MemberwiseClone();
	}
}
