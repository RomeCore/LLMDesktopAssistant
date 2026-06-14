using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using MoonSharp.Interpreter;

namespace LLMDesktopAssistant.Scripting.Lua
{
	/// <summary>
	/// Lua API for cryptography: <c>crypto.*</c>.
	/// Provides hashing (MD5, SHA1, SHA256, SHA512), HMAC, Base64 and random hex.
	/// </summary>
	[LuaApi]
	public class LuaApiCrypto : LuaApiBase
	{
		public override string? Namespace => "crypto";

		public override string? Manuals => """
			--- crypto — cryptography API

			Provides hashing, HMAC, Base64 encoding/decoding and random hex generation.

			All hash/HMAC functions accept either a string or a table of bytes as input.

			FUNCTIONS:

			--- crypto.md5(data)
			--- crypto.sha1(data)
			--- crypto.sha256(data)
			--- crypto.sha512(data)
			  Returns the hex-encoded hash of the input data.
			  Parameters:
			    - data: string or table of bytes — input data
			  Returns: string — lowercase hex digest

			--- crypto.hmac(key, data, algorithm)
			  Returns the hex-encoded HMAC of the input data.
			  Parameters:
			    - key: string or table of bytes — HMAC key
			    - data: string or table of bytes — input data
			    - algorithm: string — hash algorithm: "md5", "sha1", "sha256", "sha512"
			  Returns: string — lowercase hex digest

			--- crypto.base64_encode(data)
			  Encodes a string or bytes to Base64.
			  Parameters:
			    - data: string or table of bytes — data to encode
			  Returns: string — Base64 encoded

			--- crypto.base64_decode(str)
			  Decodes a Base64 string to plain text (UTF-8).
			  Parameters:
			    - str: string — Base64 encoded string
			  Returns: string — decoded text

			--- crypto.base64_decode_bytes(str)
			  Decodes a Base64 string to an array of bytes.
			  Parameters:
			    - str: string — Base64 encoded string
			  Returns: table — array of integers (0-255)

			--- crypto.random_hex(n)
			  Generates a random hex string of n bytes (2n hex chars).
			  Parameters:
			    - n: number — number of random bytes
			  Returns: string — hex string

			EXAMPLES:

			  -- Hash
			  print(crypto.md5("hello"))
			  print(crypto.sha256("hello"))

			  -- HMAC
			  print(crypto.hmac("key", "message", "sha256"))

			  -- Base64
			  print(crypto.base64_encode("hello world"))
			  local decoded = crypto.base64_decode("aGVsbG8gd29ybGQ=")

			  -- Random hex
			  local token = crypto.random_hex(16) -- 32 chars
			""";

		public override void Populate(Table globals, Table ns, LuaService luaService)
		{
			ns["md5"] = DynValue.NewCallback(new CallbackFunction(Md5));
			ns["sha1"] = DynValue.NewCallback(new CallbackFunction(Sha1));
			ns["sha256"] = DynValue.NewCallback(new CallbackFunction(Sha256));
			ns["sha512"] = DynValue.NewCallback(new CallbackFunction(Sha512));
			ns["hmac"] = DynValue.NewCallback(new CallbackFunction(Hmac));
			ns["base64_encode"] = DynValue.NewCallback(new CallbackFunction(Base64Encode));
			ns["base64_decode"] = DynValue.NewCallback(new CallbackFunction(Base64Decode));
			ns["base64_decode_bytes"] = DynValue.NewCallback(new CallbackFunction(Base64DecodeBytes));
			ns["random_hex"] = DynValue.NewCallback(new CallbackFunction(RandomHex));
		}

		private static DynValue Md5(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("crypto.md5(data): at least 1 argument expected.");
			var data = GetBytes(args[0]);
			return DynValue.NewString(HexEncode(MD5.HashData(data)));
		}

		private static DynValue Sha1(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("crypto.sha1(data): at least 1 argument expected.");
			var data = GetBytes(args[0]);
			return DynValue.NewString(HexEncode(SHA1.HashData(data)));
		}

		private static DynValue Sha256(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("crypto.sha256(data): at least 1 argument expected.");
			var data = GetBytes(args[0]);
			return DynValue.NewString(HexEncode(SHA256.HashData(data)));
		}

		private static DynValue Sha512(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("crypto.sha512(data): at least 1 argument expected.");
			var data = GetBytes(args[0]);
			return DynValue.NewString(HexEncode(SHA512.HashData(data)));
		}

		private static DynValue Hmac(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 3)
				throw new ScriptRuntimeException("crypto.hmac(key, data, algorithm): at least 3 arguments expected.");

			var key = GetBytes(args[0]);
			var data = GetBytes(args[1]);
			var algorithm = args[2].CastToString();
			if (algorithm == null)
				throw new ScriptRuntimeException("crypto.hmac(): third argument must be a string (algorithm: md5, sha1, sha256, sha512).");

			byte[] hash;
			switch (algorithm.ToLowerInvariant())
			{
				case "md5":
					hash = HMACMD5.HashData(key, data);
					break;
				case "sha1":
					hash = HMACSHA1.HashData(key, data);
					break;
				case "sha256":
					hash = HMACSHA256.HashData(key, data);
					break;
				case "sha512":
					hash = HMACSHA512.HashData(key, data);
					break;
				default:
					throw new ScriptRuntimeException($"crypto.hmac(): unknown algorithm '{algorithm}'. Supported: md5, sha1, sha256, sha512.");
			}

			return DynValue.NewString(HexEncode(hash));
		}

		private static DynValue Base64Encode(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("crypto.base64_encode(data): at least 1 argument expected.");
			var data = GetBytes(args[0]);
			return DynValue.NewString(Convert.ToBase64String(data));
		}

		private static DynValue Base64Decode(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("crypto.base64_decode(str): at least 1 argument expected.");
			var str = args[0].CastToString();
			if (str == null)
				throw new ScriptRuntimeException("crypto.base64_decode(): first argument must be a string.");
			var bytes = Convert.FromBase64String(str);
			return DynValue.NewString(Encoding.UTF8.GetString(bytes));
		}

		private static DynValue Base64DecodeBytes(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("crypto.base64_decode_bytes(str): at least 1 argument expected.");
			var str = args[0].CastToString();
			if (str == null)
				throw new ScriptRuntimeException("crypto.base64_decode_bytes(): first argument must be a string.");
			var bytes = Convert.FromBase64String(str);
			var result = new Table(ctx.OwnerScript);
			for (int i = 0; i < bytes.Length; i++)
				result[i + 1] = DynValue.NewNumber(bytes[i]);
			return DynValue.NewTable(result);
		}

		private static DynValue RandomHex(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("crypto.random_hex(n): at least 1 argument expected.");
			var nVal = args[0].CastToNumber();
			if (nVal == null)
				throw new ScriptRuntimeException("crypto.random_hex(): first argument must be a number.");
			int n = (int)nVal.Value;
			if (n < 0)
				throw new ScriptRuntimeException("crypto.random_hex(): n must be non-negative.");

			var bytes = new byte[n];
			RandomNumberGenerator.Fill(bytes);
			return DynValue.NewString(HexEncode(bytes));
		}

		// --- Helpers ---

		private static byte[] GetBytes(DynValue val)
		{
			if (val.Type == DataType.String)
				return Encoding.UTF8.GetBytes(val.String);
			if (val.Type == DataType.Table)
			{
				var list = new List<byte>();
				foreach (var kv in val.Table.Pairs)
				{
					if (kv.Value.Type == DataType.Number)
						list.Add((byte)kv.Value.Number);
				}
				return [.. list];
			}
			throw new ScriptRuntimeException($"Expected string or table of bytes, got {val.Type}.");
		}

		private static string HexEncode(byte[] bytes)
		{
			return Convert.ToHexStringLower(bytes);
		}
	}
}
