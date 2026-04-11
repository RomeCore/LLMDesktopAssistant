using System.Numerics;
using System.Reflection;
using RCParsing;
using RCParsing.Building;
using RCParsing.Building.ParserRules;
using RCParsing.TokenPatterns;

namespace LLMDesktopAssistant.Core.Calculation
{
	/// <summary>
	/// The calculator that performs mathematical expression parsing and evaluation.
	/// </summary>
	public static class Calculator
	{
		private static readonly Parser _exprParser = CreateMathExprParser();

		private static RuleBuilder CreateMathFunctionRule(Delegate @delegate, string funcName, string expressionName)
		{
			var builder = new RuleBuilder();

			// Two types of function are supported:
			// 1. Functions with non-optional and exact-count parameters (without 'params' keyword).
			//    Parameters can be either 'double' or 'Func<double, double>'
			// 2. Functions with one 'params double[]' parameter.

			// Valid examples:
			// double x, double y
			// Func<double, double> fn, double a, double b
			// params double[] values

			// Invalid examples:
			// double x, params double[] y
			// params Func<double, double>[] y

			var parameters = @delegate.Method.GetParameters();
			var paramCount = parameters.Length;
			var nonOptionalParamCount = parameters
				.Where(p => !p.IsOptional && !p.IsDefined(typeof(ParamArrayAttribute)))
				.Count();

			int funcType; // 0 - variable parameter count, 1 - fixed parameter count
			int minArgCount;
			int maxArgCount;

			if (paramCount == 1 && parameters[0].ParameterType == typeof(Complex[]))
			{
				funcType = 0;
				minArgCount = 0;
				maxArgCount = -1;
			}
			else if (parameters.All(p => 
									p.ParameterType == typeof(string) || 
									p.ParameterType == typeof(double) || 
									p.ParameterType == typeof(Complex) || 
									p.ParameterType == typeof(Func<MathParameterMap, Complex>)))
			{
				funcType = 1;
				minArgCount = nonOptionalParamCount;
				maxArgCount = paramCount;
			}
			else
			{
				throw new ArgumentException($"Invalid function signature for '{funcName}'.");
			}

			builder
				.Keyword(funcName)
				.Literal("(")
				.RepeatSeparated(r => r.Rule(expressionName), r => r.Literal(","), min: minArgCount, max: maxArgCount)
				.Literal(")")

				.Transform(v =>
				{
					var args = v[2].SelectArray<Func<MathParameterMap, Complex>>();
					var stringArgs = v[2].SelectArray(v => v.Text);
					bool hasFuncParameters = parameters.Any(p => p.ParameterType == typeof(Func<MathParameterMap, Complex>));

					switch (funcType)
					{
						case 0:

							return (MathParameterMap p) =>
							{
								var doubleArgs = args.Select(arg => arg(p)).ToArray();
								var value = @delegate.DynamicInvoke(doubleArgs)!;
								if (value is double d)
									return (Complex)d;
								if (value is Complex c)
									return c;
								if (value is Func<MathParameterMap, Complex> f)
									return f(p);
								throw new InvalidOperationException($"Invalid return type '{value.GetType()}' from function '{funcName}'.");
							};

						case 1:

							return (MathParameterMap p) =>
							{
								var convertedArgs = new object[parameters.Length];

								for (int i = 0; i < parameters.Length; i++)
								{
									var paramType = parameters[i].ParameterType;
									var arg = args[i];
									var strArg = stringArgs[i];

									if (paramType == typeof(double))
									{
										convertedArgs[i] = arg(p).Real;
									}
									else if (paramType == typeof(string))
									{
										convertedArgs[i] = strArg;
									}
									else if (paramType == typeof(Complex))
									{
										convertedArgs[i] = arg(p);
									}
									else if (paramType == typeof(Func<MathParameterMap, Complex>))
									{
										convertedArgs[i] = arg;
									}
								}

								var value = @delegate.DynamicInvoke(convertedArgs)!;
								if (value is double d)
									return (Complex)d;
								if (value is Complex c)
									return c;
								if (value is Func<MathParameterMap, Complex> f)
									return f(p);
								throw new InvalidOperationException($"Invalid return type '{value.GetType()}' from function '{funcName}'.");
							};

						default:
							throw new Exception(); // Will not be thrown.
					}
				});

			return builder;
		}

		private static Parser CreateMathExprParser(Action<ParserBuilder>? builderAction = null)
		{
			var builder = new ParserBuilder();

			builder.Settings
				.SkipWhitespaces();

			// Basic terms

			builder.CreateRule("number")
				.Number<double>(NumberFlags.UnsignedScientific)

				.Transform(v =>
				{
					var value = v.GetIntermediateValue<double>();
					return (MathParameterMap p) => new Complex(value, 0);
				});

			builder.CreateRule("imaginary")
				.KeywordIgnoreCase("i")

				.Transform(v =>
				{
					return (MathParameterMap p) => new Complex(0, 1);
				});

			builder.CreateRule("const")
				.KeywordChoiceIgnoreCase("nan", "pi", "π", "inf", "infinity", "∞", "eps", "epsilon", "ε", "phi", "φ", "tau", "τ", "g", "e", "c", "gamma")

				.Transform(v =>
				{
					var str = v.GetIntermediateValue<string>();
					var value = str switch
					{
						"nan" => double.NaN,
						"pi" => Math.PI,
						"π" => Math.PI,
						"inf" => double.PositiveInfinity,
						"infinity" => double.PositiveInfinity,
						"∞" => double.PositiveInfinity,
						"eps" => double.Epsilon,
						"epsilon" => double.Epsilon,
						"ε" => double.Epsilon,
						"phi" => 1.618033988749895,
						"φ" => 1.618033988749895,
						"tau" => 6.283185307179586,
						"τ" => 6.283185307179586,
						"g" => 9.81,
						"e" => double.E,
						"c" => 299792458.0,
						"gamma" => 0.5772156649015328,
						_ => throw new Exception() // Will not be thrown
					};
					return (MathParameterMap p) => new Complex(value, 0);
				});

			builder.CreateRule("variable")
				.Identifier()

				.Transform(v =>
				{
					var str = v.Text;
					return (MathParameterMap p) => p[str];
				});

			// Functions

			var funcChoiceRuleHolder = builder.CreateRule("func");
			var funcChoiceRule = new BuildableChoiceParserRule();
			funcChoiceRule.ParsedValueFactory = v => v.GetValue(0);
			funcChoiceRuleHolder.BuildingRule = funcChoiceRule;

			void AddFunction(Delegate @delegate, string name)
			{
				funcChoiceRule.Choices.Add(CreateMathFunctionRule(@delegate, name, "expr").BuildingRule!.Value);
			}

			AddFunction((Complex x) => Complex.Sin(x), "sin");
			AddFunction((Complex x) => Complex.Sinh(x), "sinh");
			AddFunction((Complex x) => Complex.Asin(x), "asin");
			AddFunction((double x) => Math.Asinh(x), "asinh");
			AddFunction((Complex x) => Complex.Cos(x), "cos");
			AddFunction((Complex x) => Complex.Cosh(x), "cosh");
			AddFunction((Complex x) => Complex.Acos(x), "acos");
			AddFunction((double x) => Math.Acosh(x), "acosh");
			AddFunction((Complex x) => Complex.Tan(x), "tan");
			AddFunction((Complex x) => Complex.Tanh(x), "tanh");
			AddFunction((Complex x) => Complex.Atan(x), "atan");
			AddFunction((double x) => Math.Atanh(x), "atanh");
			AddFunction((double y, double x) => Math.Atan2(y, x), "atan2");

			AddFunction((Complex x) => Complex.Sin(x * Math.PI / 180.0), "sind");
			AddFunction((Complex x) => Complex.Cos(x * Math.PI / 180.0), "cosd");
			AddFunction((Complex x) => Complex.Tan(x * Math.PI / 180.0), "tand");
			AddFunction((Complex x) => Complex.Asin(x) * 180.0 / Math.PI, "asind");
			AddFunction((Complex x) => Complex.Acos(x) * 180.0 / Math.PI, "acosd");
			AddFunction((Complex x) => Complex.Atan(x) * 180.0 / Math.PI, "atand");

			AddFunction((Complex x) => Complex.Log(x), "ln");
			AddFunction((Complex x) => Complex.Log10(x), "log");
			AddFunction((Complex x) => Complex.Log(x, 2), "log2");
			AddFunction((Complex a, double b) => Complex.Log(a, b), "logb");
			AddFunction((Complex x) => Complex.Exp(x), "exp");
			AddFunction((Complex x, Complex y) => Complex.Pow(x, y), "pow");
			AddFunction((Complex x) => Complex.Sqrt(x), "sqrt");
			AddFunction((double x) => Math.Cbrt(x), "cbrt");
			AddFunction((double x) => (double)Math.Sign(x), "sign");
			AddFunction((Complex x) => Complex.Abs(x), "abs");
			AddFunction((Complex x) => x.Magnitude, "mag");
			AddFunction((Complex x) => Complex.Conjugate(x), "conjugate");
			AddFunction((double x) => Math.Ceiling(x), "ceil");
			AddFunction((double x) => Math.Floor(x), "floor");
			AddFunction((double x) => Math.Round(x), "round");
			AddFunction((double x) => Math.Truncate(x), "trunc");
			AddFunction((Complex[] vals) => vals.Max(v => v.Real), "max");
			AddFunction((Complex[] vals) => vals.Min(v => v.Real), "min");
			AddFunction((Complex[] vals) => vals.Aggregate(Complex.Zero, (a, b) => Complex.MaxMagnitude(a, b)), "maxmag");
			AddFunction((Complex[] vals) => vals.Aggregate(Complex.Zero, (a, b) => Complex.MinMagnitude(a, b)), "minmag");
			AddFunction((double a, double b) => (double)(a % b), "mod");

			AddFunction((double value) => Gamma(value), "gamma");
			AddFunction((Complex value) => CompGamma(value), "compgamma");
			AddFunction((double value) => Factorial((int)value), "factorial");
			AddFunction((Func<MathParameterMap, Complex> f, string variable, double a, double b) => Integral(f, variable, a, b), "integral");
			AddFunction((Func<MathParameterMap, Complex> f, string variable, double x) => Derivative(f, variable, x), "derivative");

			// The expressions

			builder.CreateRule("term")
				.Choice(
					b => b.Rule("number"),
					b => b.Rule("imaginary"),
					b => b.Rule("func"),
					b => b.Rule("const"),
					b => b.Rule("variable"),

					b => b.Literal("(").Rule("expr").Literal(")")
						.Transform(v => v.GetValue<Func<MathParameterMap, Complex>>(index: 1)),

					b => b.Literal("|").Rule("expr").Literal("|")
						.Transform(v => (MathParameterMap p) => Complex.Abs(v.GetValue<Func<MathParameterMap, Complex>>(index: 1)(p)))
				)
				.Optional(b => b.Literal("!")) // Factorial
				.Optional(b => b.Rule("term")) // For support 2(2 + 3) or 2i

				.Transform(v =>
				{
					var firstTerm = v.GetValue<Func<MathParameterMap, Complex>>(index: 0);
					var secondTerm = v.TryGetValue<Func<MathParameterMap, Complex>>(index: 2);
					var hasFactorial = v[1].Length != 0;

					var result = firstTerm;
					if (secondTerm != null)
					{
						result = (MathParameterMap p) => firstTerm(p) * secondTerm(p);
					}
					if (hasFactorial)
					{
						var inner = result;
						result = (MathParameterMap p) => Factorial((int)inner(p).Real);
					}
					return result;
				});

			builder.CreateRule("op_pow")
				.OneOrMoreSeparated(b => b.Rule("term"), b => b.Literal("^"))

				.TransformFoldRight<Func<MathParameterMap, Complex>, Func<MathParameterMap, Complex>>((l, r) =>
				{
					return (x) => Complex.Pow(l(x), r(x));
				});

			builder.CreateRule("op_pre")
				.ZeroOrMore(b => b.LiteralChoice("+", "-"))
				.Rule("op_pow")

				.Transform(v =>
				{
					var operators = v.SelectArray<string>(index: 0);
					var value = v.GetValue<Func<MathParameterMap, Complex>>(index: 1);

					for (int i = operators.Length - 1; i >= 0; i--)
					{
						var op = operators[i];
						var currentValue = value;
						value = op switch
						{
							"-" => (x) => -currentValue(x),
							_ => currentValue
						};
					}

					return value;
				});

			builder.CreateRule("op_mul")
				.OneOrMoreSeparated(b => b.Rule("op_pre"), b => b.LiteralChoice("*", "/"), includeSeparatorsInResult: true)

				.TransformFoldLeft<Func<MathParameterMap, Complex>, string, Func<MathParameterMap, Complex>>((l, op, r) =>
				{
					return op == "*" ? (MathParameterMap x) => l(x) * r(x) : (MathParameterMap x) => l(x) / r(x);
				});

			builder.CreateRule("op_add")
				.OneOrMoreSeparated(b => b.Rule("op_mul"), b => b.LiteralChoice("+", "-"), includeSeparatorsInResult: true)

				.TransformFoldLeft<Func<MathParameterMap, Complex>, string, Func<MathParameterMap, Complex>>((l, op, r) =>
				{
					return op == "+" ? (MathParameterMap x) => l(x) + r(x) : (MathParameterMap x) => l(x) - r(x);
				});

			builder.CreateRule("expr")
				.Rule("op_add");

			builder.CreateMainRule()
				.Rule("expr").EOF().TransformSelect(0);

			builderAction?.Invoke(builder);
			return builder.Build();
		}

		static double Gamma(double x)
		{
			if (x <= 0.0)
				return double.NaN;

			double[] p = { 0.99999999999980993, 676.5203681218851, -1259.1392167224028,
				   771.32342877765313, -176.61502916214059, 12.507343278686905,
				   -0.13857109526572012, 9.9843695780195716e-6, 1.5056327351493116e-7 };

			int g = 7;
			if (x < 0.5)
				return Math.PI / (Math.Sin(Math.PI * x) * Gamma(1.0 - x));

			x -= 1.0;
			double a = p[0];
			double t = x + g + 0.5;

			for (int i = 1; i < p.Length; i++)
				a += p[i] / (x + i);

			return Math.Sqrt(2.0 * Math.PI) * Math.Pow(t, x + 0.5) * Math.Exp(-t) * a;
		}

		static Complex CompGamma(Complex x)
		{
			if (x.Magnitude <= 0.0)
				return double.NaN;

			double[] p = { 0.99999999999980993, 676.5203681218851, -1259.1392167224028,
				   771.32342877765313, -176.61502916214059, 12.507343278686905,
				   -0.13857109526572012, 9.9843695780195716e-6, 1.5056327351493116e-7 };

			int g = 7;
			if (x.Magnitude < 0.5)
				return Math.PI / (Complex.Sin(Math.PI * x) * CompGamma(1.0 - x));

			x -= 1.0;
			Complex a = p[0];
			Complex t = x + g + 0.5;

			for (int i = 1; i < p.Length; i++)
				a += p[i] / (x + i);

			return Complex.Sqrt(2.0 * Math.PI) * Complex.Pow(t, x + 0.5) * Complex.Exp(-t) * a;
		}

		static double Factorial(int n)
		{
			if (n < 0) return double.NaN;
			if (n == 0) return 1.0;

			double result = 1.0;
			for (long i = 2; i <= n; i++)
			{
				result *= i;
				if (result > double.PositiveInfinity)
					return result;
			}

			return result;
		}

		static Func<MathParameterMap, Complex> Integral(Func<MathParameterMap, Complex> f, string variable, double a, double b)
		{
			if (double.IsInfinity(a) || double.IsInfinity(b))
			{
				return InfiniteIntegral(f, variable, a, b);
			}

			return FiniteIntegral(f, variable, a, b);
		}

		static Func<MathParameterMap, Complex> FiniteIntegral(Func<MathParameterMap, Complex> f, string variable, double a, double b)
		{
			return (MathParameterMap p) =>
			{
				const int steps = 1000;
				double step = (b - a) / steps;
				double sum = 0.0;

				for (int i = 0; i < steps; i++)
				{
					double x0 = a + i * step;
					double x1 = x0 + step;
					double xm = (x0 + x1) / 2;

					sum += step / 6 * (f(p.WithReplaced(variable, x0)).Real + 4 * f(p.WithReplaced(variable, xm)).Real + f(p.WithReplaced(variable, x1))).Real;
				}

				return sum;
			};
		}

		static Func<MathParameterMap, Complex> InfiniteIntegral(Func<MathParameterMap, Complex> f, string variable, double a, double b)
		{
			Func<MathParameterMap, Complex> transformedF;
			double newA, newB;

			if (double.IsNegativeInfinity(a) && double.IsPositiveInfinity(b))
			{
				// ∫_{-∞}^{∞} f(x) dx = ∫_{-1}^{1} f(t/(1-t²)) * (1+t²)/(1-t²)² dt
				transformedF = (p) =>
				{
					var t = p[variable].Real;
					if (Math.Abs(t) >= 1) return 0;
					double x = t / (1 - t * t);
					double jacobian = (1 + t * t) / ((1 - t * t) * (1 - t * t));
					return f(p.WithReplaced(variable, x)) * jacobian;
				};
				newA = -0.999999;
				newB = 0.999999;
			}
			else if (double.IsNegativeInfinity(a))
			{
				// ∫_{-∞}^{b} f(x) dx = ∫_{0}^{1} f(b + t/(t-1)) * 1/(1-t)² dt
				transformedF = (p) =>
				{
					var t = p[variable].Real;
					if (t >= 1) return 0;
					double x = b + t / (t - 1);
					double jacobian = 1 / ((1 - t) * (1 - t));
					return f(p.WithReplaced(variable, x)) * jacobian;
				};
				newA = 0;
				newB = 0.999999;
			}
			else if (double.IsPositiveInfinity(b))
			{
				// ∫_{a}^{∞} f(x) dx = ∫_{0}^{1} f(a + t/(1-t)) * 1/(1-t)² dt
				transformedF = (p) =>
				{
					var t = p[variable].Real;
					if (t >= 1) return 0;
					double x = a + t / (1 - t);
					double jacobian = 1 / ((1 - t) * (1 - t));
					return f(p.WithReplaced(variable, x)) * jacobian;
				};
				newA = 0;
				newB = 0.999999;
			}
			else
			{
				throw new ArgumentException("Invalid integration limits");
			}

			return FiniteIntegral(transformedF, variable, newA, newB);
		}

		static Func<MathParameterMap, Complex> Derivative(Func<MathParameterMap, Complex> f, string variable, double x0)
		{
			const double h = 1e-8;
			return (MathParameterMap p) =>
			{
				var fp = f(p.WithReplaced(variable, x0 + h)).Real;
				var fm = f(p.WithReplaced(variable, x0 - h)).Real;
				return (fp - fm) / (2 * h);
			};
		}

		/// <summary>
		/// Parses a mathematical function from the given expression string.
		/// </summary>
		/// <param name="expression">The string to parse.</param>
		/// <returns>The parsed function.</returns>
		public static Func<MathParameterMap, Complex> ParseFunction(string expression)
		{
			return _exprParser.Parse<Func<MathParameterMap, Complex>>(expression);
		}

		/// <summary>
		/// Parses a value from the given expression string.
		/// </summary>
		/// <param name="expression">The string to parse.</param>
		/// <returns>The parsed value.</returns>
		public static Complex ParseValue(string expression)
		{
			return _exprParser.Parse<Func<MathParameterMap, Complex>>(expression)(new MathParameterMap(0));
		}
	}
}