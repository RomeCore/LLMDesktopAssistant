using Avalonia.Controls;
using LLMDesktopAssistant.Controls;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.StructuredValues.Reactive;
using Material.Icons;

namespace LLMDesktopAssistant.Addons.MVVM.Elements
{
	/// <summary>
	/// A collapsible card block that edits the parameters declared by the parameter schema of the addon.
	/// </summary>
	/// <remarks>
	/// The editor control is created on the first expansion of the block and not before: building the
	/// editor materializes the parameter values, which for an editable card means writing a change.
	/// A card the user never touched must stay clean, so the work is deferred until the user asks for it.
	/// </remarks>
	/// <typeparam name="TAddon">The type of the addon the block belongs to.</typeparam>
	/// <typeparam name="TChange">The type of the change (override) object of that addon.</typeparam>
	public class AddonCardParametersBlock<TAddon, TChange> : AddonCardBlock
		where TAddon : AddonChangedBase<TAddon, TChange>
		where TChange : AddonChangeBase, new()
	{
		private readonly AddonCardContext<TAddon, TChange> _context;
		private readonly ContentControl _host = new();

		private ReactiveNodeValue? _readOnlyValue;

		/// <summary>
		/// Initializes a new instance of the <see cref="AddonCardParametersBlock{TAddon, TChange}"/> class.
		/// </summary>
		/// <param name="context">The addon, its change set and the page that owns the card.</param>
		public AddonCardParametersBlock(AddonCardContext<TAddon, TChange> context)
		{
			ArgumentNullException.ThrowIfNull(context);

			_context = context;

			Visibility = AddonCardBlockVisibility.Collapsible;
			Title = Locale.GetKey("card.parameters");
			ToggleIcon = MaterialIconKind.Tune;
			ToggleToolTip = Locale.GetKey("card.parameters.toggle");
			Content = _host;
		}

		/// <inheritdoc/>
		public override bool IsExpanded
		{
			get => base.IsExpanded;
			set
			{
				base.IsExpanded = value;

				if (value)
					EnsureEditor();
			}
		}

		private void EnsureEditor()
		{
			if (_host.Content is not null)
				return;

			var schema = _context.Addon.ParameterSchema;
			if (schema is null)
				return;

			var value = schema.Root.CreateOrFixValue(_context.Change?.Parameters ?? _readOnlyValue, []);

			if (_context.CanEdit)
				_context.EnsureChange().Parameters = value;
			else
				_readOnlyValue = value;

			_host.Content = new ParameterEditorControl
			{
				Schema = schema,
				Value = value
			};
		}
	}
}
