using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Services.Prompting;

namespace LLMDesktopAssistant.Prompting.Context
{
	/// <summary>
	/// Base class for prompt sections: delegates all work to the four typed services:
	/// state provider, state renderer, delta provider and delta renderer.
	/// </summary>
	public abstract class PromptAnchoredSectionBase<TState, TDelta> : IPromptAnchoredSectionProvider
		where TState : PromptSectionStateBase
		where TDelta : PromptSectionDeltaBase
	{
		private readonly IPromptSectionStateProvider<TState> _stateProvider;
		private readonly IPromptSectionStateRenderer<TState> _stateRenderer;
		private readonly IPromptSectionDeltaProvider<TState, TDelta> _deltaProvider;
		private readonly IPromptSectionDeltaRenderer<TDelta> _deltaRenderer;

		/// <inheritdoc/>
		public Type StateType => typeof(TState);

		/// <inheritdoc/>
		public Type DeltaType => typeof(TDelta);

		protected PromptAnchoredSectionBase(IServiceProvider services)
		{
			_stateProvider = services.GetRequiredService<IPromptSectionStateProvider<TState>>();
			_stateRenderer = services.GetRequiredService<IPromptSectionStateRenderer<TState>>();
			_deltaProvider = services.GetRequiredService<IPromptSectionDeltaProvider<TState, TDelta>>();
			_deltaRenderer = services.GetRequiredService<IPromptSectionDeltaRenderer<TDelta>>();
		}

		/// <inheritdoc/>
		public PromptSectionStateBase? CaptureState(ChatAgentDescriptor agent) => _stateProvider.CaptureState(agent);

		/// <inheritdoc/>
		public SystemPromptSnapshot RenderState(PromptSectionStateBase state) => _stateRenderer.Render((TState)state);

		/// <inheritdoc/>
		public string RenderDelta(PromptSectionDeltaBase delta) => _deltaRenderer.Render((TDelta)delta);

		/// <inheritdoc/>
		public PromptSectionDeltaBase? CalculateDelta(PromptSectionStateBase? anchorState,
			IEnumerable<PromptSectionDeltaBase> existingDeltas, PromptSectionStateBase? actualState,
			EffectiveChatContext context)
			=> _deltaProvider.CalculateDelta((TState?)anchorState, existingDeltas.Cast<TDelta>(), (TState?)actualState, context);
	}
}
