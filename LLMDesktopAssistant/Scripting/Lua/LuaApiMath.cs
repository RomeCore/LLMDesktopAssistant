using System.Numerics;
using LLMDesktopAssistant.Calculation;
using LLMDesktopAssistant.Calculation.Ast;
using MoonSharp.Interpreter;

namespace LLMDesktopAssistant.Scripting.Lua
{
	/// <summary>
	/// Lua API extending the built-in math library: <c>math.*</c>.
	/// Adds expression evaluation, variable binding, complex numbers,
	/// numerical integration, differentiation, and equation solving.
	/// </summary>
	[LuaApi]
	public class LuaApiMath : LuaApiBase
	{
		public override string? Namespace => "math";

		public override string? Manuals => """
			--- math — extended mathematics API

			Supplements the built-in Lua math library with expression parsing/evaluation,
			complex numbers, numerical integration/differentiation, and equation solving.

			NEW FUNCTIONS:

			--- math.parse(expression)
			  Parses a mathematical expression string into a callable function.
			  The returned function accepts an optional table of variable values.
			  This is more efficient than math.evaluate for repeated evaluation.
			  Parameters:
			    - expression: string — expression to parse (e.g. "x^2 + 2*x + 1")
			  Returns: function(vars) -> number|table
			  Example:
			    local f = math.parse("x^2 + 2*x + 1")
			    f({x = 0})  → 1
			    f({x = 1})  → 4
			    f({x = 2})  → 9

			--- math.evaluate(expression, [vars])
			  Evaluates a mathematical expression string with optional variable bindings.
			  Supports complex numbers, integrals, derivatives, all math functions.
			  Parameters:
			    - expression: string — expression to evaluate
			    - vars: table (optional) — variable bindings {x = 3, y = 5}
			  Returns: number (real) or table {real, imag} for complex results
			  Examples:
			    math.evaluate("sin(pi/4)^2 + cos(pi/4)^2")     → 1.0
			    math.evaluate("2*x + y", {x = 3, y = 5})       → 11.0
			    math.evaluate("(3+4i) * (2-5i")                → {real=26, imag=-7}
			    math.evaluate("integral(x^2, x, 0, 10)")       → 333.333...

			--- math.solve(equation, [variable], [rangeStart], [rangeEnd], [scanStep])
			  Solves f(x) = 0 numerically using the bisection method.
			  Parameters:
			    - equation: string — equation to solve (e.g. "x^2 - 5*x + 6 = 0")
			    - variable: string (optional, default "x")
			    - rangeStart: number (optional, default -100)
			    - rangeEnd: number (optional, default 100)
			    - scanStep: number (optional, default 0.1)
			  Returns: table — array of found roots (may be empty)
			  Examples:
			    math.solve("x^2 - 5*x + 6 = 0")  → {2.0, 3.0}
			    math.solve("cos(x) - x", "x", 0, 10, 0.01)  → {~0.739}

			--- math.integral(f, a, b, [steps])
			  Numerically integrates a Lua function over [a, b].
			  Parameters:
			    - f: function(x) — function to integrate
			    - a: number — lower bound
			    - b: number — upper bound
			    - steps: number (optional, default 1000) — Simpson steps
			  Returns: number
			  Example:
			    math.integral(function(x) return x^2 end, 0, 10)  → 333.333...

			--- math.derivative(f, x)
			  Numerically differentiates a Lua function at point x.
			  Parameters:
			    - f: function(x) — function to differentiate
			    - x: number — point to evaluate at
			  Returns: number
			  Example:
			    math.derivative(function(x) return x^3 end, 5)  → ~75.0

			--- math.complex(real, imag)
			  Creates a complex number as a table {real, imag}.
			--- math.mag(z)
			  Magnitude of a complex number (or absolute value of a real).
			--- math.conj(z)
			  Conjugate of a complex number.
			--- math.real(z)
			  Real part of a complex number (or the number itself).
			--- math.imag(z)
			  Imaginary part of a complex number.

			--- math.log2(x)       — logarithm base 2
			--- math.logb(x, base) — logarithm with arbitrary base
			--- math.cbrt(x)       — cube root
			--- math.gamma(x)      — Gamma function (Lanczos approximation)
			--- math.factorial(n)  — factorial (integer n >= 0)
			--- math.round(x)      — round to nearest integer
			--- math.trunc(x)      — truncate toward zero
			--- math.sind(x)       — sine (x in degrees)
			--- math.cosd(x)       — cosine (x in degrees)
			--- math.tand(x)       — tangent (x in degrees)

			CONSTANTS (via math.constants):

			--- math.constants.pi      — π = 3.141592653589793
			--- math.constants.e       — e = 2.718281828459045
			--- math.constants.phi     — φ = 1.618033988749895 (golden ratio)
			--- math.constants.tau     — τ = 6.283185307179586
			--- math.constants.gamma   — γ = 0.5772156649015328 (Euler-Mascheroni)
			--- math.constants.g       — g = 9.81 (gravitational acceleration)
			--- math.constants.c       — c = 299792458 (speed of light)
			--- math.constants.halfpi  — π/2 = 1.5707963267948966
			--- math.constants.sqrt2   — √2 = 1.4142135623730951
			--- math.constants.sqrt3   — √3 = 1.7320508075688772
			--- math.constants.ln2     — ln(2) = 0.6931471805599453

			EXAMPLES:

			  -- Parse once, evaluate many times
			  local f = math.parse("x^2 + 2*x + 1")
			  for i = 0, 5 do
			    print(f({x = i}))
			  end  → 1, 4, 9, 16, 25, 36

			  -- Evaluate with variables
			  math.evaluate("a * b + c", {a = 2, b = 3, c = 1})  → 7

			  -- Complex arithmetic
			  local z = math.evaluate("(1+2i) / (3+4i)")
			  print(z.real, z.imag)  → 0.44, 0.08

			  -- Solve equations
			  local roots = math.solve("x^3 - 6*x^2 + 11*x - 6 = 0")
			  for _, r in ipairs(roots) do print(r) end  → 1, 2, 3

			  -- Numerical methods with Lua functions
			  local area = math.integral(function(x) return math.sin(x) end, 0, math.pi)
			  print(area)  → ~2.0

			  local slope = math.derivative(function(x) return x^3 end, 5)
			  print(slope)  → ~75.0

			  -- Constants
			  print(math.constants.phi)   → 1.61803...
			  print(math.constants.sqrt2) → 1.41421...
			""";

		public override void Populate(Table globals, Table ns)
		{
			// Core
			ns["parse"] = DynValue.NewCallback(new CallbackFunction(Parse));
			ns["evaluate"] = DynValue.NewCallback(new CallbackFunction(Evaluate));
			ns["solve"] = DynValue.NewCallback(new CallbackFunction(Solve));

			// Numerical methods
			ns["integral"] = DynValue.NewCallback(new CallbackFunction(Integral));
			ns["derivative"] = DynValue.NewCallback(new CallbackFunction(Derivative));

			// Complex number support
			ns["complex"] = DynValue.NewCallback(new CallbackFunction(MakeComplex));
			ns["mag"] = DynValue.NewCallback(new CallbackFunction(Mag));
			ns["conj"] = DynValue.NewCallback(new CallbackFunction(Conj));
			ns["real"] = DynValue.NewCallback(new CallbackFunction(Real));
			ns["imag"] = DynValue.NewCallback(new CallbackFunction(Imag));

			// Extra math functions
			ns["log2"] = DynValue.NewCallback(new CallbackFunction(Log2));
			ns["logb"] = DynValue.NewCallback(new CallbackFunction(LogB));
			ns["cbrt"] = DynValue.NewCallback(new CallbackFunction(Cbrt));
			ns["gamma"] = DynValue.NewCallback(new CallbackFunction(Gamma));
			ns["factorial"] = DynValue.NewCallback(new CallbackFunction(Factorial));
			ns["round"] = DynValue.NewCallback(new CallbackFunction(Round));
			ns["trunc"] = DynValue.NewCallback(new CallbackFunction(Trunc));
			ns["sind"] = DynValue.NewCallback(new CallbackFunction(Sind));
			ns["cosd"] = DynValue.NewCallback(new CallbackFunction(Cosd));
			ns["tand"] = DynValue.NewCallback(new CallbackFunction(Tand));

			// Constants table
			var constants = new Table(globals.OwnerScript);
			constants["pi"] = DynValue.NewNumber(Math.PI);
			constants["e"] = DynValue.NewNumber(double.E);
			constants["phi"] = DynValue.NewNumber(1.618033988749895);
			constants["tau"] = DynValue.NewNumber(6.283185307179586);
			constants["gamma"] = DynValue.NewNumber(0.5772156649015328);
			constants["g"] = DynValue.NewNumber(9.81);
			constants["c"] = DynValue.NewNumber(299792458.0);
			constants["inf"] = DynValue.NewNumber(double.PositiveInfinity);
			constants["nan"] = DynValue.NewNumber(double.NaN);
			constants["halfpi"] = DynValue.NewNumber(Math.PI / 2);
			constants["sqrt2"] = DynValue.NewNumber(1.4142135623730951);
			constants["sqrt3"] = DynValue.NewNumber(1.7320508075688772);
			constants["ln2"] = DynValue.NewNumber(0.6931471805599453);
			ns["constants"] = DynValue.NewTable(constants);
		}

		// ============================================================
		//  math.parse(expression) → function(vars)
		// ============================================================

		private DynValue Parse(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("math.parse(expression, [vars]): at least 1 argument expected.");

			var exprStr = args[0].CastToString()
				?? throw new ScriptRuntimeException("math.parse(expression): expression must be a string.");

			MathEntity parsed;
			try
			{
				parsed = MathExpressionParser.Parse(exprStr);
			}
			catch (Exception ex)
			{
				throw new ScriptRuntimeException($"math.parse() error: {ex.Message}");
			}

			// Capture the parsed expression in a closure
			var script = ctx.OwnerScript;
			var capturedEntity = parsed;

			var closure = DynValue.NewCallback(new CallbackFunction((c, a) =>
			{
				var evalCtx = new MathEvaluationContext();

				// If variables table provided, push it as a scope
				if (a.Count >= 1 && a[0].Type == DataType.Table)
				{
					var vars = a[0].Table;
					var scope = new Dictionary<string, MathEntity>();
					foreach (var key in vars.Keys)
					{
						var keyStr = key.CastToString();
						if (keyStr == null) continue;

						var val = vars.Get(key);
						var num = val.CastToNumber();
						if (num != null)
						{
							scope[keyStr] = new ConstantEntity(new Complex(num.Value, 0));
							continue;
						}

						// Support complex table {real, imag}
						if (val.Type == DataType.Table)
						{
							var t = val.Table;
							var re = t.Get("real").CastToNumber() ?? 0;
							var im = t.Get("imag").CastToNumber() ?? 0;
							scope[keyStr] = new ConstantEntity(new Complex(re, im));
							continue;
						}
					}
					evalCtx.PushScope(scope);
				}

				try
				{
					var result = capturedEntity.Evaluate(evalCtx);
					var complex = result.ToComplexOrThrow();

					if (complex.Imaginary == 0)
						return DynValue.NewNumber(complex.Real);

					var tbl = new Table(script);
					tbl["real"] = DynValue.NewNumber(complex.Real);
					tbl["imag"] = DynValue.NewNumber(complex.Imaginary);
					return DynValue.NewTable(tbl);
				}
				catch (Exception ex)
				{
					throw new ScriptRuntimeException($"math.parse() evaluation error: {ex.Message}");
				}
				finally
				{
					if (a.Count >= 1 && a[0].Type == DataType.Table)
						evalCtx.PopScope();
				}
			}));

			return closure;
		}

		// ============================================================
		//  math.evaluate(expression, [vars])
		// ============================================================

		private DynValue Evaluate(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("math.evaluate(expression, [vars]): at least 1 argument expected.");

			var exprStr = args[0].CastToString()
				?? throw new ScriptRuntimeException("math.evaluate(expression, [vars]): expression must be a string.");

			try
			{
				var parsed = MathExpressionParser.Parse(exprStr);
				var evalCtx = new MathEvaluationContext();

				// Optional variables table
				if (args.Count >= 2 && args[1].Type == DataType.Table)
				{
					var vars = args[1].Table;
					var scope = new Dictionary<string, MathEntity>();
					foreach (var key in vars.Keys)
					{
						var keyStr = key.CastToString();
						if (keyStr == null) continue;

						var val = vars.Get(key);
						var num = val.CastToNumber();
						if (num != null)
						{
							scope[keyStr] = new ConstantEntity(new Complex(num.Value, 0));
							continue;
						}

						if (val.Type == DataType.Table)
						{
							var t = val.Table;
							var re = t.Get("real").CastToNumber() ?? 0;
							var im = t.Get("imag").CastToNumber() ?? 0;
							scope[keyStr] = new ConstantEntity(new Complex(re, im));
							continue;
						}
					}
					evalCtx.PushScope(scope);
				}

				MathEntity result;
				try
				{
					result = parsed.Evaluate(evalCtx);
				}
				finally
				{
					if (args.Count >= 2 && args[1].Type == DataType.Table)
						evalCtx.PopScope();
				}

				var complex = result.ToComplexOrThrow();

				if (complex.Imaginary == 0)
					return DynValue.NewNumber(complex.Real);

				var tbl = new Table(ctx.OwnerScript);
				tbl["real"] = DynValue.NewNumber(complex.Real);
				tbl["imag"] = DynValue.NewNumber(complex.Imaginary);
				return DynValue.NewTable(tbl);
			}
			catch (Exception ex)
			{
				throw new ScriptRuntimeException($"math.evaluate() error: {ex.Message}");
			}
		}

		// ============================================================
		//  math.solve(equation, [variable], [rangeStart], [rangeEnd], [scanStep])
		// ============================================================

		private DynValue Solve(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("math.solve(equation, ...): at least 1 argument expected.");

			var equation = args[0].CastToString()
				?? throw new ScriptRuntimeException("math.solve(equation, ...): equation must be a string.");

			string variable = "x";
			double rangeStart = -100.0;
			double rangeEnd = 100.0;
			double scanStep = 0.1;

			if (args.Count >= 2 && args[1].Type != DataType.Nil)
				variable = args[1].CastToString() ?? "x";
			if (args.Count >= 3 && args[2].Type != DataType.Nil)
				rangeStart = args[2].CastToNumber() ?? -100.0;
			if (args.Count >= 4 && args[3].Type != DataType.Nil)
				rangeEnd = args[3].CastToNumber() ?? 100.0;
			if (args.Count >= 5 && args[4].Type != DataType.Nil)
				scanStep = args[4].CastToNumber() ?? 0.1;

			try
			{
				// Support "expr = 0" or just "expr"
				string expressionStr = equation;
				if (equation.Contains('='))
				{
					var parts = equation.Split('=', 2);
					expressionStr = $"({parts[0].Trim()}) - ({parts[1].Trim()})";
				}

				var parsed = MathExpressionParser.Parse(expressionStr);
				var roots = MathEquationSolver.FindRoots(parsed, variable, rangeStart, rangeEnd, scanStep);

				var resultTable = new Table(ctx.OwnerScript);
				for (int i = 0; i < roots.Length; i++)
					resultTable[i + 1] = DynValue.NewNumber(roots[i]);

				return DynValue.NewTable(resultTable);
			}
			catch (Exception ex)
			{
				throw new ScriptRuntimeException($"math.solve() error: {ex.Message}");
			}
		}

		// ============================================================
		//  math.integral(f, a, b, [steps])
		// ============================================================

		private DynValue Integral(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 3)
				throw new ScriptRuntimeException("math.integral(f, a, b, [steps]): at least 3 arguments expected.");

			if (args[0].Type != DataType.Function)
				throw new ScriptRuntimeException("math.integral(f, a, b, [steps]): first argument must be a function.");

			var func = args[0].Function;
			double a = args[1].CastToNumber()
				?? throw new ScriptRuntimeException("math.integral(f, a, b, [steps]): a must be a number.");
			double b = args[2].CastToNumber()
				?? throw new ScriptRuntimeException("math.integral(f, a, b, [steps]): b must be a number.");

			int steps = NumericalIntegration.DefaultSteps;
			if (args.Count >= 4 && args[3].Type != DataType.Nil)
				steps = (int)(args[3].CastToNumber() ?? NumericalIntegration.DefaultSteps);

			double h = (b - a) / steps;
			double sum = 0;

			for (int i = 0; i < steps; i++)
			{
				double x0 = a + i * h;
				double x1 = x0 + h;
				double xm = (x0 + x1) / 2;

				double f0 = CallFunction(func, x0);
				double fm = CallFunction(func, xm);
				double f1 = CallFunction(func, x1);

				sum += h / 6 * (f0 + 4 * fm + f1);
			}

			return DynValue.NewNumber(sum);
		}

		// ============================================================
		//  math.derivative(f, x)
		// ============================================================

		private DynValue Derivative(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("math.derivative(f, x): at least 2 arguments expected.");

			if (args[0].Type != DataType.Function)
				throw new ScriptRuntimeException("math.derivative(f, x): first argument must be a function.");

			var func = args[0].Function;
			double x = args[1].CastToNumber()
				?? throw new ScriptRuntimeException("math.derivative(f, x): x must be a number.");

			const double h = NumericalDifferentiation.DefaultStepSize;
			double fp = CallFunction(func, x + h);
			double fm = CallFunction(func, x - h);

			return DynValue.NewNumber((fp - fm) / (2 * h));
		}

		// ============================================================
		//  Complex number utilities
		// ============================================================

		private DynValue MakeComplex(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("math.complex(real, imag): 2 arguments expected.");

			double real = args[0].CastToNumber()
				?? throw new ScriptRuntimeException("math.complex(): real must be a number.");
			double imag = args[1].CastToNumber()
				?? throw new ScriptRuntimeException("math.complex(): imag must be a number.");

			var tbl = new Table(ctx.OwnerScript);
			tbl["real"] = DynValue.NewNumber(real);
			tbl["imag"] = DynValue.NewNumber(imag);
			return DynValue.NewTable(tbl);
		}

		private DynValue Mag(ScriptExecutionContext ctx, CallbackArguments args)
		{
			var (re, im) = GetComplex(args, "math.mag");
			return DynValue.NewNumber(Math.Sqrt(re * re + im * im));
		}

		private DynValue Conj(ScriptExecutionContext ctx, CallbackArguments args)
		{
			var (re, im) = GetComplex(args, "math.conj");
			var tbl = new Table(ctx.OwnerScript);
			tbl["real"] = DynValue.NewNumber(re);
			tbl["imag"] = DynValue.NewNumber(-im);
			return DynValue.NewTable(tbl);
		}

		private DynValue Real(ScriptExecutionContext ctx, CallbackArguments args)
		{
			var (re, _) = GetComplex(args, "math.real");
			return DynValue.NewNumber(re);
		}

		private DynValue Imag(ScriptExecutionContext ctx, CallbackArguments args)
		{
			var (_, im) = GetComplex(args, "math.imag");
			return DynValue.NewNumber(im);
		}

		// ============================================================
		//  Extra math functions
		// ============================================================

		private DynValue Log2(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("math.log2(x): 1 argument expected.");
			double x = args[0].CastToNumber()
				?? throw new ScriptRuntimeException("math.log2(x): x must be a number.");
			return DynValue.NewNumber(Math.Log(x, 2));
		}

		private DynValue LogB(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("math.logb(x, base): 2 arguments expected.");
			double x = args[0].CastToNumber()
				?? throw new ScriptRuntimeException("math.logb(x, base): x must be a number.");
			double baseVal = args[1].CastToNumber()
				?? throw new ScriptRuntimeException("math.logb(x, base): base must be a number.");
			return DynValue.NewNumber(Math.Log(x, baseVal));
		}

		private DynValue Cbrt(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("math.cbrt(x): 1 argument expected.");
			double x = args[0].CastToNumber()
				?? throw new ScriptRuntimeException("math.cbrt(x): x must be a number.");
			return DynValue.NewNumber(Math.Cbrt(x));
		}

		private DynValue Gamma(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("math.gamma(x): 1 argument expected.");
			double x = args[0].CastToNumber()
				?? throw new ScriptRuntimeException("math.gamma(x): x must be a number.");
			return DynValue.NewNumber(ComputeGamma(x));
		}

		private DynValue Factorial(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("math.factorial(n): 1 argument expected.");
			double n = args[0].CastToNumber()
				?? throw new ScriptRuntimeException("math.factorial(n): n must be a number.");
			return DynValue.NewNumber(ComputeFactorial((int)n));
		}

		private DynValue Round(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("math.round(x): 1 argument expected.");
			double x = args[0].CastToNumber()
				?? throw new ScriptRuntimeException("math.round(x): x must be a number.");
			return DynValue.NewNumber(Math.Round(x));
		}

		private DynValue Trunc(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("math.trunc(x): 1 argument expected.");
			double x = args[0].CastToNumber()
				?? throw new ScriptRuntimeException("math.trunc(x): x must be a number.");
			return DynValue.NewNumber(Math.Truncate(x));
		}

		private DynValue Sind(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("math.sind(x): 1 argument expected.");
			double x = args[0].CastToNumber()
				?? throw new ScriptRuntimeException("math.sind(x): x must be a number.");
			return DynValue.NewNumber(Math.Sin(x * Math.PI / 180.0));
		}

		private DynValue Cosd(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("math.cosd(x): 1 argument expected.");
			double x = args[0].CastToNumber()
				?? throw new ScriptRuntimeException("math.cosd(x): x must be a number.");
			return DynValue.NewNumber(Math.Cos(x * Math.PI / 180.0));
		}

		private DynValue Tand(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("math.tand(x): 1 argument expected.");
			double x = args[0].CastToNumber()
				?? throw new ScriptRuntimeException("math.tand(x): x must be a number.");
			return DynValue.NewNumber(Math.Tan(x * Math.PI / 180.0));
		}

		// ============================================================
		//  Helpers
		// ============================================================

		private static (double real, double imag) GetComplex(CallbackArguments args, string funcName)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException($"{funcName}(z): 1 argument expected.");

			if (args[0].Type == DataType.Table)
			{
				var tbl = args[0].Table;
				var real = tbl.Get("real").CastToNumber() ?? 0;
				var imag = tbl.Get("imag").CastToNumber() ?? 0;
				return (real, imag);
			}

			var num = args[0].CastToNumber();
			if (num != null)
				return (num.Value, 0);

			throw new ScriptRuntimeException($"{funcName}(z): argument must be a number or complex table.");
		}

		private static double CallFunction(Closure func, double x)
		{
			var result = func.Call(DynValue.NewNumber(x));
			var num = result.CastToNumber();
			if (num == null)
				throw new ScriptRuntimeException("Function returned non-numeric value.");
			return num.Value;
		}

		private static double ComputeGamma(double x)
		{
			// Lanczos approximation for real gamma
			if (x <= 0) return double.NaN;
			double[] p = {
				0.99999999999980993, 676.5203681218851, -1259.1392167224028,
				771.32342877765313, -176.61502916214059, 12.507343278686905,
				-0.13857109526572012, 9.9843695780195716e-6, 1.5056327351493116e-7
			};
			if (x < 0.5)
				return Math.PI / (Math.Sin(Math.PI * x) * ComputeGamma(1 - x));
			x -= 1;
			double a = p[0];
			for (int i = 1; i < p.Length; i++) a += p[i] / (x + i);
			double t = x + 7.5;
			return Math.Sqrt(2 * Math.PI) * Math.Pow(t, x + 0.5) * Math.Exp(-t) * a;
		}

		private static double ComputeFactorial(int n)
		{
			if (n < 0) return double.NaN;
			double r = 1;
			for (long i = 2; i <= n && !double.IsPositiveInfinity(r); i++) r *= i;
			return r;
		}
	}
}
