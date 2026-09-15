namespace LLMDesktopAssistant.Addons.MVVM
{
	/// <summary>
	/// A simple <see cref="IAddonCardChange"/> implementation that delegates the reset to a callback.
	/// Factories may derive from this class to expose additional bound state.
	/// </summary>
	public class AddonCardChange : AddonCardElementBase, IAddonCardChange
	{
		/// <inheritdoc/>
		public object? Content { get; init; }

		/// <inheritdoc/>
		public bool IsShownLeft { get; init; }

		/// <inheritdoc/>
		public virtual bool IsChanged
		{
			get;
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// The callback that removes the override from the addon configuration.
		/// When <see langword="null"/>, <see cref="Reset"/> does nothing.
		/// </summary>
		public Action? ResetAction { get; init; }

		/// <inheritdoc/>
		public virtual void Reset() => ResetAction?.Invoke();
	}
}
