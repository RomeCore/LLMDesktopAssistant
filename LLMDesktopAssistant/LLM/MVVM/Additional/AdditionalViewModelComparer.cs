namespace LLMDesktopAssistant.LLM.Domain
{
	public class AdditionalViewModelComparer : IComparer<AdditionalMessageViewModel>
	{
		public static AdditionalViewModelComparer Instance { get; } = new();

		public int Compare(AdditionalMessageViewModel? x, AdditionalMessageViewModel? y)
		{
			if (x is null && y is null) return 0;
			if (x is null) return -1;
			if (y is null) return 1;

			return x.Order.CompareTo(y.Order);
		}
	}
}