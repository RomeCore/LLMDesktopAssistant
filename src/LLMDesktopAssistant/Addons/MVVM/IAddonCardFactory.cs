namespace LLMDesktopAssistant.Addons.MVVM
{
	public interface IAddonCardFactory<T>
	{
		AddonCardViewModel Create(T addon);

		AddonCardViewModel Create<TChange>(T addon, TChange change);
	}
}
