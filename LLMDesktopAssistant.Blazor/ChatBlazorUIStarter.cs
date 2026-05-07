using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Serilog;

namespace LLMDesktopAssistant.Blazor
{
	[ChatService(typeof(IChatBlazorUIStarter))]
	public class ChatBlazorUIStarter(
		IServiceProvider chatServices
	) : Disposable, IChatBlazorUIStarter
	{
		private WebApplication? _webApp;
		private SemaphoreSlim _stateSemaphore = new SemaphoreSlim(1, 1);

		public bool IsRunning { get; private set; } = false;

		public void Start()
		{
			if (IsRunning)
				throw new InvalidOperationException("Chat Blazor UI is already running.");
			if (_stateSemaphore.CurrentCount == 0)
				throw new InvalidOperationException("Chat Blazor UI is already starting or stopping.");
			_stateSemaphore.Wait();

			Log.Information("Starting Chat Blazor UI...");

			try
			{
				var builder = WebApplication.CreateBuilder(new WebApplicationOptions
				{
					WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
					ContentRootPath = Directory.GetCurrentDirectory(),
					EnvironmentName = "Development",
				});

				builder.Services.AddServerSideBlazor();

				// Add services to the container.
				builder.Services.AddRazorComponents()
					.AddInteractiveServerComponents();

				var app = builder.Build();
				_webApp = app;

				// Configure the HTTP request pipeline.
				if (app.Environment.IsDevelopment())
				{
					// app.UseWebAssemblyDebugging();
				}
				else
				{
					app.UseExceptionHandler("/Error");
					app.UseHsts();
				}

				app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

				app.UseAntiforgery();

				app.MapStaticAssets("LLMDesktopAssistant.Blazor.staticwebassets.endpoints.json");

				app.MapRazorComponents<Components.App>()
					.AddInteractiveServerRenderMode();

				app.RunAsync();

				IsRunning = true;
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to start Chat Blazor UI.");
			}
			finally
			{
				_stateSemaphore.Release();
			}
		}

		public void Stop()
		{
			if (!IsRunning)
				throw new InvalidOperationException("Chat Blazor UI is not running.");
			if (_stateSemaphore.CurrentCount == 0)
				throw new InvalidOperationException("Chat Blazor UI is already starting or stopping.");
			_stateSemaphore.Wait();

			Log.Information("Stopping Chat Blazor UI...");

			try
			{
				_webApp?.StopAsync();
				_webApp = null;
				IsRunning = false;
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to start Chat Blazor UI.");
			}
			finally
			{
				_stateSemaphore.Release();
			}
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing && _webApp != null)
				Stop();
		}
	}
}
