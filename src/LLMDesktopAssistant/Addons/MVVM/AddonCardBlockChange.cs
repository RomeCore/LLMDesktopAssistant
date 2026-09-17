using CommunityToolkit.Mvvm.Input;

namespace LLMDesktopAssistant.Addons.MVVM
{
	/// <summary>
	/// A simple <see cref="IAddonCardBlockChange"/> implementation that delegates the reset to a callback.
	/// Factories may derive from this class to expose additional bound state.
	/// </summary>
	public class AddonCardBlockChange : AddonCardBlock, IAddonCardBlockChange
	{
		/// <inheritdoc/>
		public virtual bool IsChanged
		{
			get;
			set => SetProperty(ref field, value);
		}

		/// <inheritdoc/>
		public ICommand? ResetCommand => field ??= new RelayCommand(ResetCore);

		protected virtual void ResetCore()
		{
		}
	}
}
