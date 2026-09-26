using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.LLM.Services.Prompting;

namespace LLMDesktopAssistant.Prompting.Context.Providers.Identity
{
	/// <summary>
	/// Delta provider of the core prompt section (stub: no deltas yet).
	/// </summary>
	[ChatService(typeof(IPromptSectionDeltaProvider<IdentitySectionState, IdentitySectionDelta>))]
	public class IdentityDeltaProvider(
		
	) : IPromptSectionDeltaProvider<IdentitySectionState, IdentitySectionDelta>
	{
		/// <inheritdoc/>
		public IdentitySectionDelta? CalculateDelta(IdentitySectionState? anchorState,
			IEnumerable<IdentitySectionDelta> existingDeltas, EffectiveChatContext context)
		{
			string? currentPersona = anchorState?.Persona;
			string? currentSpecialization = anchorState?.Specialization;
			string? currentNickname = anchorState?.AssistantNickname;

			foreach (var delta in existingDeltas)
			{
				if (delta.PersonaChanged)
					currentPersona = delta.NewPersona;
				if (delta.SpecializationChanged)
					currentSpecialization = delta.NewSpecialization;
				if (delta.NicknameChanged)
					currentNickname = delta.NewAssistantNickname;
			}



			return null;
		}
	}
}
