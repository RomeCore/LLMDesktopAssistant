using LLMDesktopAssistant.Services.Instances;
using MoonSharp.Interpreter;

namespace LLMDesktopAssistant.Scripting.Lua
{
	/// <summary>
	/// Lua API for showing toast notifications in the UI: <c>dass.toasts.*</c>.
	/// Thin wrapper over <see cref="IToastService"/>.
	/// </summary>
	[LuaApi(chatScoped: false)]
	public class LuaApiToasts : LuaApiBase
	{
		private readonly IToastService _toastService;

		public override string? Namespace => "dass.toasts";

		public override string? Manuals => """
			--- dass.toasts — toast notification API

			Shows toast notifications (popup messages) in the application UI.

			FUNCTIONS:

			--- dass.toasts.info(title, [description], [durationSeconds])
			  Shows an informational toast.
			  Parameters:
			    - title: string — toast title (supports Markdown)
			    - description: string (optional) — additional text
			    - durationSeconds: number (optional, default 5) — auto-dismiss seconds
			  Returns: nil

			--- dass.toasts.warning(title, [description], [durationSeconds])
			  Shows a warning toast (default duration: 6 seconds).

			--- dass.toasts.error(title, [description], [durationSeconds])
			  Shows an error toast (default duration: 8 seconds).

			--- dass.toasts.success(title, [description], [durationSeconds])
			  Shows a success toast (default duration: 5 seconds).

			EXAMPLES:

			  dass.toasts.info("File saved!")
			  dass.toasts.warning("Disk almost full", "Only 500 MB remaining", 10)
			  dass.toasts.error("Connection lost", "Check your network settings")
			  dass.toasts.success("**Build complete**", "All 42 tests passed")
			""";

		public LuaApiToasts(IToastService toastService)
		{
			_toastService = toastService;
		}

		public override void Populate(Table globals, Table ns, LuaService luaService)
		{
			ns["info"] = DynValue.NewCallback(new CallbackFunction((ctx, args) =>
			{
				_toastService.ShowInfo(
					GetString(args, 0, "title"),
					GetOptionalString(args, 1),
					GetOptionalNumber(args, 2) ?? 5.0);
				return DynValue.Nil;
			}));

			ns["warning"] = DynValue.NewCallback(new CallbackFunction((ctx, args) =>
			{
				_toastService.ShowWarning(
					GetString(args, 0, "title"),
					GetOptionalString(args, 1),
					GetOptionalNumber(args, 2) ?? 6.0);
				return DynValue.Nil;
			}));

			ns["error"] = DynValue.NewCallback(new CallbackFunction((ctx, args) =>
			{
				_toastService.ShowError(
					GetString(args, 0, "title"),
					GetOptionalString(args, 1),
					GetOptionalNumber(args, 2) ?? 8.0);
				return DynValue.Nil;
			}));

			ns["success"] = DynValue.NewCallback(new CallbackFunction((ctx, args) =>
			{
				_toastService.ShowSuccess(
					GetString(args, 0, "title"),
					GetOptionalString(args, 1),
					GetOptionalNumber(args, 2) ?? 5.0);
				return DynValue.Nil;
			}));
		}

		private static string GetString(CallbackArguments args, int index, string paramName)
		{
			if (args.Count <= index)
				throw new ScriptRuntimeException($"dass.toasts: argument #{index + 1} ({paramName}) is required.");
			return args[index].CastToString()
				?? throw new ScriptRuntimeException($"dass.toasts: argument #{index + 1} ({paramName}) must be a string.");
		}

		private static string? GetOptionalString(CallbackArguments args, int index)
		{
			return args.Count > index ? args[index].CastToString() : null;
		}

		private static double? GetOptionalNumber(CallbackArguments args, int index)
		{
			return args.Count > index ? args[index].CastToNumber() : null;
		}
	}
}
