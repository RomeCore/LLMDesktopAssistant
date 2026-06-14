using System;
using System.Collections.Generic;
using System.IO;
using LLMDesktopAssistant.Services.Instances;
using MoonSharp.Interpreter;
using SixLabors.ImageSharp;

namespace LLMDesktopAssistant.Scripting.Lua
{
	/// <summary>
	/// Lua API for image loading, manipulation and saving: <c>image.*</c>.
	/// Provides access to SixLabors.ImageSharp functionality.
	/// Registered in the global namespace as "image".
	/// </summary>
	[LuaApi]
	public class LuaApiImage : LuaApiBase
	{
		public override string? Namespace => "image";

		public override string? Manuals => """
		--- image — Image loading, manipulation and saving API

		Provides functions to load, create, manipulate and save images.
		Uses SixLabors.ImageSharp under the hood.
		All image objects are UserData with methods.

		FUNCTIONS:

		--- image.load(path) -> Image
		  Loads an image from a file path.
		  Parameters:
		    - path: string — path to the image file
		  Returns: Image object (UserData)

		--- image.load_base64(base64) -> Image
		  Loads an image from a base64-encoded string.
		  Parameters:
		    - base64: string — base64-encoded image data
		  Returns: Image object (UserData)

		--- image.load_url(url) -> Image
		  Loads an image from a URL.
		  Parameters:
		    - url: string — URL of the image
		  Returns: Image object (UserData)

		--- image.create(width, height, [color]) -> Image
		  Creates a new blank image.
		  Parameters:
		    - width: number — width in pixels
		    - height: number — height in pixels
		    - color: string (optional) — background color (hex "#ff0000", name "red", etc.)
		  Returns: Image object (UserData)

		--- image.from_bytes(bytes, [format]) -> Image
		  Creates an image from raw byte data.
		  Parameters:
		    - bytes: string (binary data as Lua string)
		    - format: string (optional) — image format hint
		  Returns: Image object (UserData)

		--- image.info(path) -> table
		  Gets image metadata without loading the full image.
		  Parameters:
		    - path: string — path to the image file
		  Returns: table with fields: width, height, format

		--- image.formats() -> table
		  Returns a list of supported image formats.
		  Returns: array of strings

		IMAGE OBJECT METHODS:

		--- img:save(path, [options])
		  Saves the image to a file.
		  Parameters:
		    - path: string — output file path
		    - options: table (optional):
		      - quality: number — JPEG/WebP quality (1-100, default 85)

		--- img:to_base64([format]) -> string
		  Converts the image to a base64-encoded string.
		  Parameters:
		    - format: string (optional) — output format ("png", "jpg", "webp", default: original)
		  Returns: string

		--- img:resize(width, height, [options])
		  Resizes the image.
		  Parameters:
		    - width: number — new width
		    - height: number — new height
		    - options: table (optional):
		      - mode: string — "stretch" (default), "contain" (pad), "cover" (crop)

		--- img:crop(x, y, width, height)
		  Crops the image to the specified rectangle.

		--- img:rotate(degrees)
		  Rotates the image by the specified degrees.

		--- img:flip(direction)
		  Flips the image.
		  Parameters:
		    - direction: string — "horizontal", "vertical", or "both"

		--- img:clone() -> Image
		  Creates a deep copy of the image.

		--- img:dispose()
		  Manually disposes the underlying image resources.
		  Automatically called by garbage collector.

		IMAGE OBJECT PROPERTIES (read-only):
		  - img.width: number — image width in pixels
		  - img.height: number — image height in pixels
		  - img.format: string — image format ("png", "jpg", "webp", etc.)

		EXAMPLES:

		  -- Load, resize and save
		  local img = image.load("photo.jpg")
		  img:resize(800, 600, { mode = "cover" })
		  img:save("thumb.jpg", { quality = 80 })

		  -- Load from URL and attach to message
		  local img = image.load_url("https://example.com/chart.png")
		  local system_message = {
		      role = "system",
		      content = "You are a helpful assistant."
		  }
		  local user_message = {
		      role = "user",
		      content = "Check out this chart!",
		      attachments = { img }
		  }
		  local response_messages = dass.agents.execute({ system_message, user_message })

		  -- Create an image from scratch
		  local img = image.create(100, 100, "#ff0000")
		  img:save("red_square.png")

		  -- Get metadata without full load
		  local info = image.info("large.png")
		  print(info.width, info.height, info.format)

		NOTES:
		  - Image objects must be disposed manually with :dispose() or will be
		    disposed automatically by Lua garbage collector.
		  - Supported formats: png, jpg/jpeg, webp, gif, bmp
		  - base64 format is auto-detected from file header bytes.
		  - All manipulation methods modify the image in-place. Use :clone() first
		    if you need to preserve the original.
		""";

		private readonly FileAccessService _fileAccess;

		public LuaApiImage(FileAccessService fileAccess)
		{
			_fileAccess = fileAccess;
		}

		public override void Populate(Table globals, Table ns, LuaService luaService)
		{
			// Register LuaImage as UserData type for MoonSharp
			UserData.RegisterType<LuaImage>();

			ns["load"] = DynValue.NewCallback(new CallbackFunction(Load));
			ns["load_base64"] = DynValue.NewCallback(new CallbackFunction(LoadBase64));
			ns["load_url"] = DynValue.NewCallback(new CallbackFunction(LoadUrl));
			ns["create"] = DynValue.NewCallback(new CallbackFunction(Create));
			ns["from_bytes"] = DynValue.NewCallback(new CallbackFunction(FromBytes));
			ns["info"] = DynValue.NewCallback(new CallbackFunction(Info));
			ns["formats"] = DynValue.NewCallback(new CallbackFunction(Formats));
		}

		private DynValue Load(ScriptExecutionContext ctx, CallbackArguments args)
		{
			try
			{
				if (args.Count < 1)
					throw new ScriptRuntimeException("image.load(path): at least 1 argument expected.");
				var path = args[0].CastToString();
				if (string.IsNullOrEmpty(path))
					throw new ScriptRuntimeException("image.load(path): path must be a non-empty string.");

				var luaImage = LuaImage.Load(_fileAccess, path);
				return UserData.Create(luaImage);
			}
			catch (ScriptRuntimeException) { throw; }
			catch (Exception ex)
			{
				throw new ScriptRuntimeException($"image.load() error: {ex.Message}");
			}
		}

		private DynValue LoadBase64(ScriptExecutionContext ctx, CallbackArguments args)
		{
			try
			{
				if (args.Count < 1)
					throw new ScriptRuntimeException("image.load_base64(base64): at least 1 argument expected.");
				var base64 = args[0].CastToString();
				if (string.IsNullOrEmpty(base64))
					throw new ScriptRuntimeException("image.load_base64(base64): base64 must be a non-empty string.");

				var luaImage = LuaImage.LoadBase64(_fileAccess, base64);
				return UserData.Create(luaImage);
			}
			catch (ScriptRuntimeException) { throw; }
			catch (Exception ex)
			{
				throw new ScriptRuntimeException($"image.load_base64() error: {ex.Message}");
			}
		}

		private DynValue LoadUrl(ScriptExecutionContext ctx, CallbackArguments args)
		{
			try
			{
				if (args.Count < 1)
					throw new ScriptRuntimeException("image.load_url(url): at least 1 argument expected.");
				var url = args[0].CastToString();
				if (string.IsNullOrEmpty(url))
					throw new ScriptRuntimeException("image.load_url(url): url must be a non-empty string.");

				var luaImage = LuaImage.LoadUrl(_fileAccess, url);
				return UserData.Create(luaImage);
			}
			catch (ScriptRuntimeException) { throw; }
			catch (Exception ex)
			{
				throw new ScriptRuntimeException($"image.load_url() error: {ex.Message}");
			}
		}

		private DynValue Create(ScriptExecutionContext ctx, CallbackArguments args)
		{
			try
			{
				if (args.Count < 2)
					throw new ScriptRuntimeException("image.create(width, height, [color]): at least 2 arguments expected.");

				var widthNum = args[0].CastToNumber();
				var heightNum = args[1].CastToNumber();
				if (widthNum == null || heightNum == null)
					throw new ScriptRuntimeException("image.create(width, height, [color]): width and height must be numbers.");
				var width = (int)widthNum.Value;
				var height = (int)heightNum.Value;
				string? color = args.Count > 2 ? args[2].CastToString() : null;

				var luaImage = LuaImage.Create(_fileAccess, width, height, color);
				return UserData.Create(luaImage);
			}
			catch (ScriptRuntimeException) { throw; }
			catch (Exception ex)
			{
				throw new ScriptRuntimeException($"image.create() error: {ex.Message}");
			}
		}

		private DynValue FromBytes(ScriptExecutionContext ctx, CallbackArguments args)
		{
			try
			{
				if (args.Count < 1)
					throw new ScriptRuntimeException("image.from_bytes(bytes, [format]): at least 1 argument expected.");

				var bytesStr = args[0].CastToString();
				if (string.IsNullOrEmpty(bytesStr))
					throw new ScriptRuntimeException("image.from_bytes(bytes): bytes must be a non-empty string.");

				// Convert Lua string (which is just binary data as a string) to byte[]
				var bytes = System.Text.Encoding.UTF8.GetBytes(bytesStr);
				// Actually, in MoonSharp, strings can hold binary data directly
				// We should use the raw string data
				bytes = System.Text.Encoding.Default.GetBytes(bytesStr);

				string? format = args.Count > 1 ? args[1].CastToString() : null;
				var luaImage = LuaImage.FromBytes(_fileAccess, bytes, format);
				return UserData.Create(luaImage);
			}
			catch (ScriptRuntimeException) { throw; }
			catch (Exception ex)
			{
				throw new ScriptRuntimeException($"image.from_bytes() error: {ex.Message}");
			}
		}

		private DynValue Info(ScriptExecutionContext ctx, CallbackArguments args)
		{
			try
			{
				if (args.Count < 1)
					throw new ScriptRuntimeException("image.info(path): at least 1 argument expected.");
				var path = args[0].CastToString();
				if (string.IsNullOrEmpty(path))
					throw new ScriptRuntimeException("image.info(path): path must be a non-empty string.");

				var fullPath = _fileAccess.AccessPath(path);
				return DynValue.NewTable(LuaImage.GetInfo(ctx.GetScript(), fullPath));
			}
			catch (ScriptRuntimeException) { throw; }
			catch (Exception ex)
			{
				throw new ScriptRuntimeException($"image.info() error: {ex.Message}");
			}
		}

		private DynValue Formats(ScriptExecutionContext ctx, CallbackArguments args)
		{
			try
			{
				return DynValue.NewTable(LuaImage.GetFormats(ctx.GetScript()));
			}
			catch (Exception ex)
			{
				throw new ScriptRuntimeException($"image.formats() error: {ex.Message}");
			}
		}
	}
}
