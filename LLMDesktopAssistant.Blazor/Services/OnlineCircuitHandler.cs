using Microsoft.AspNetCore.Components.Server.Circuits;

namespace LLMDesktopAssistant.Blazor.Services
{
	[WebUIService(typeof(CircuitHandler), IsScoped = true)]
	public class OnlineCircuitHandler(IOnlineStateService onlineState, IUserStateService userState) : CircuitHandler
	{
		public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
		{
			var currentUser = userState.GetCurrentUser();
			if (currentUser != null)
				onlineState.EnterSession(currentUser.Login);
			return base.OnCircuitOpenedAsync(circuit, cancellationToken);
		}

		public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
		{
			var currentUser = userState.GetCurrentUser();
			if (currentUser != null)
				onlineState.LeaveSession(currentUser.Login);
			return base.OnCircuitClosedAsync(circuit, cancellationToken);
		}
	}
}
