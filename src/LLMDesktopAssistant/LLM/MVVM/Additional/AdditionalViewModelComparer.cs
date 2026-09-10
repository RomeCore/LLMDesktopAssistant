namespace LLMDesktopAssistant.LLM.MVVM.Additional
{
	public class AdditionalViewModelComparer : IComparer<AdditionalChatData>
	{
		public static AdditionalViewModelComparer Instance { get; } = new();

		public int Compare(AdditionalChatData? x, AdditionalChatData? y)
		{
			if (x is null && y is null) return 0;
			if (x is null) return -1;
			if (y is null) return 1;

			return x.Order.CompareTo(y.Order);
		}
	}
}