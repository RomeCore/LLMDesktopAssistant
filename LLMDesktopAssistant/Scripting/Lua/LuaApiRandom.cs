using System;
using System.Collections.Generic;
using System.Text;
using MoonSharp.Interpreter;

namespace LLMDesktopAssistant.Scripting.Lua
{
	/// <summary>
	/// Lua API for random number generation and sampling: <c>random.*</c>.
	/// Registered in the global namespace as "random".
	/// </summary>
	[LuaApi]
	public class LuaApiRandom : LuaApiBase
	{
		public override string? Namespace => "random";

		public override string? Manuals => """
			--- random — random number generation and sampling API

			Provides various randomisation functions backed by System.Random.

			FUNCTIONS:

			--- random.number([min], max)
			  Returns a random floating-point number.
			  If called with one argument: returns [0, max].
			  If called with two arguments: returns [min, max].
			  Parameters:
			    - min: number (optional) — lower bound (inclusive), default 0
			    - max: number — upper bound (inclusive)
			  Returns: number

			--- random.integer([min], max)
			  Returns a random integer.
			  If called with one argument: returns [1, max].
			  If called with two arguments: returns [min, max] (both inclusive).
			  Parameters:
			    - min: number (optional) — lower bound (inclusive), default 1
			    - max: number — upper bound (inclusive)
			  Returns: integer

			--- random.boolean([chance])
			  Returns true or false with the given probability.
			  Parameters:
			    - chance: number (optional, default 0.5) — probability of true, 0 to 1
			  Returns: boolean

			--- random.choice(array, [n])
			  Returns random element(s) from an array table.
			  If n is nil or 1: returns a single random element.
			  If n > 1: returns an array of n unique random elements.
			  Parameters:
			    - array: table — array to pick from (1-based, non-sparse)
			    - n: number (optional) — number of elements to pick
			  Returns: element or array of elements
			  Throws if n > #array.

			--- random.shuffle(array)
			  Shuffles the array in-place using Fisher-Yates algorithm.
			  Parameters:
			    - array: table — array to shuffle (1-based)
			  Returns: the same table (mutated)

			--- random.guid()
			  Generates a random GUID/UUID string.
			  Returns: string — e.g. "550e8400-e29b-41d4-a716-446655440000"

			--- random.bytes(n)
			  Returns an array of n random bytes (0-255).
			  Parameters:
			    - n: number — number of bytes to generate
			  Returns: table — array of integers

			--- random.seed([seed])
			  Seeds the random number generator.
			  If called without arguments, seeds with Guid.NewGuid().
			  Parameters:
			    - seed: number (optional) — explicit seed value
			  Returns: nil

			EXAMPLES:

			  -- Dice roll
			  local roll = random.integer(1, 6)

			  -- Random float
			  local pct = random.number(0, 1)

			  -- 30% chance
			  if random.boolean(0.3) then print("lucky!") end

			  -- Pick one
			  local pick = random.choice({"rock", "paper", "scissors"})

			  -- Pick many
			  local picks = random.choice({"a","b","c","d","e"}, 3)

			  -- Shuffle
			  local deck = {1,2,3,4,5,6}
			  random.shuffle(deck)

			  -- GUID
			  print(random.guid())
			""";

		private Random _rng = new();

		public override void Populate(Table globals, Table ns)
		{
			ns["number"] = DynValue.NewCallback(new CallbackFunction(Number));
			ns["integer"] = DynValue.NewCallback(new CallbackFunction(Integer));
			ns["boolean"] = DynValue.NewCallback(new CallbackFunction(Boolean));
			ns["choice"] = DynValue.NewCallback(new CallbackFunction(Choice));
			ns["shuffle"] = DynValue.NewCallback(new CallbackFunction(Shuffle));
			ns["guid"] = DynValue.NewCallback(new CallbackFunction(Guid));
			ns["bytes"] = DynValue.NewCallback(new CallbackFunction(Bytes));
			ns["seed"] = DynValue.NewCallback(new CallbackFunction(Seed));
		}

		private DynValue Number(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count == 0)
				throw new ScriptRuntimeException("random.number([min], max): at least 1 argument expected.");

			if (args.Count == 1)
			{
				var max = args[0].CastToNumber();
				if (max == null)
					throw new ScriptRuntimeException("random.number(max): max must be a number.");
				return DynValue.NewNumber(_rng.NextDouble() * max.Value);
			}

			var min = args[0].CastToNumber();
			var max2 = args[1].CastToNumber();
			if (min == null || max2 == null)
				throw new ScriptRuntimeException("random.number(min, max): both arguments must be numbers.");
			return DynValue.NewNumber(min.Value + _rng.NextDouble() * (max2.Value - min.Value));
		}

		private DynValue Integer(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count == 0)
				throw new ScriptRuntimeException("random.integer([min], max): at least 1 argument expected.");

			if (args.Count == 1)
			{
				var max = args[0].CastToNumber();
				if (max == null)
					throw new ScriptRuntimeException("random.integer(max): max must be a number.");
				return DynValue.NewNumber(_rng.Next(1, (int)max.Value + 1));
			}

			var min = args[0].CastToNumber();
			var max2 = args[1].CastToNumber();
			if (min == null || max2 == null)
				throw new ScriptRuntimeException("random.integer(min, max): both arguments must be numbers.");
			return DynValue.NewNumber(_rng.Next((int)min.Value, (int)max2.Value + 1));
		}

		private DynValue Boolean(ScriptExecutionContext ctx, CallbackArguments args)
		{
			double chance = 0.5;
			if (args.Count > 0)
			{
				var c = args[0].CastToNumber();
				if (c == null)
					throw new ScriptRuntimeException("random.boolean([chance]): chance must be a number between 0 and 1.");
				chance = c.Value;
				if (chance < 0 || chance > 1)
					throw new ScriptRuntimeException("random.boolean([chance]): chance must be between 0 and 1.");
			}
			return DynValue.NewBoolean(_rng.NextDouble() < chance);
		}

		private DynValue Choice(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("random.choice(array, [n]): at least 1 argument expected.");

			var array = args[0];
			if (array.Type != DataType.Table)
				throw new ScriptRuntimeException("random.choice(): first argument must be a table (array).");

			var length = array.Table.Length;
			if (length == 0)
				throw new ScriptRuntimeException("random.choice(): array is empty.");

			// If n is nil or 1, return a single element
			if (args.Count < 2 || args[1].IsNil())
			{
				return array.Table.Get(_rng.Next(1, length + 1));
			}

			var nVal = args[1].CastToNumber();
			if (nVal == null)
				throw new ScriptRuntimeException("random.choice(array, n): n must be a number.");
			int n = (int)nVal.Value;

			if (n < 1)
				throw new ScriptRuntimeException("random.choice(array, n): n must be at least 1.");
			if (n > length)
				throw new ScriptRuntimeException($"random.choice(array, n): n ({n}) exceeds array length ({length}).");

			// Fisher-Yates partial shuffle — pick first n elements
			var script = ctx.OwnerScript;
			var result = new Table(script);

			// Copy indices
			var indices = new int[length];
			for (int i = 0; i < length; i++)
				indices[i] = i + 1;

			// Partial Fisher-Yates
			for (int i = 0; i < n; i++)
			{
				int j = _rng.Next(i, length);
				// swap
				int temp = indices[i];
				indices[i] = indices[j];
				indices[j] = temp;

				result[i + 1] = array.Table.Get(indices[i]);
			}

			return DynValue.NewTable(result);
		}

		private DynValue Shuffle(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("random.shuffle(array): at least 1 argument expected.");

			var array = args[0];
			if (array.Type != DataType.Table)
				throw new ScriptRuntimeException("random.shuffle(): first argument must be a table (array).");

			var t = array.Table;
			int len = t.Length;
			if (len <= 1)
				return array;

			// Fisher-Yates in-place
			var keys = t.Keys;
			for (int i = len; i >= 2; i--)
			{
				int j = _rng.Next(1, i + 1);
				// swap t[i] and t[j]
				var tmp = t.Get(i);
				t.Set(i, t.Get(j));
				t.Set(j, tmp);
			}

			return array;
		}

		private static DynValue Guid(ScriptExecutionContext ctx, CallbackArguments args)
		{
			return DynValue.NewString(System.Guid.NewGuid().ToString());
		}

		private DynValue Bytes(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("random.bytes(n): at least 1 argument expected.");

			var nVal = args[0].CastToNumber();
			if (nVal == null)
				throw new ScriptRuntimeException("random.bytes(n): n must be a number.");
			int n = (int)nVal.Value;
			if (n < 0)
				throw new ScriptRuntimeException("random.bytes(n): n must be non-negative.");

			var buffer = new byte[n];
			_rng.NextBytes(buffer);

			var result = new Table(ctx.OwnerScript);
			for (int i = 0; i < n; i++)
				result[i + 1] = DynValue.NewNumber(buffer[i]);

			return DynValue.NewTable(result);
		}

		private DynValue Seed(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count == 0 || args[0].IsNil())
			{
				_rng = new();
				return DynValue.Nil;
			}

			var seedVal = args[0].CastToNumber();
			if (seedVal == null)
				throw new ScriptRuntimeException("random.seed([seed]): seed must be a number.");
			_rng = new Random((int)seedVal.Value);
			return DynValue.Nil;
		}
	}
}
