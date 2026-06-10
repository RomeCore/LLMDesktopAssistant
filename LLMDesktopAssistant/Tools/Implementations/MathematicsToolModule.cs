using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Spreadsheet;
using LLMDesktopAssistant.Calculation;
using LLMDesktopAssistant.Calculation.Ast;
using LLMDesktopAssistant.Services;
using RCLargeLanguageModels.Tools;
using RCParsing;

namespace LLMDesktopAssistant.Tools.Implementations
{
	[ToolModule]
	public class MathematicsToolModule : ToolModule
	{
		public MathematicsToolModule()
		{
			AddTool(Calculate,
				new ToolInitializationInfo
				{
					Name = "math-calculate",
					Description = """
						Evaluate a mathematical expression. Examples:

						1 + 2 * -(3^2 / e)
						sin(pi/2) + cos(pi/3)
						integral(x^2 + 1, x, 0, 10)
						derivative(x^2 + 1, x, 5)
						sin(9+2i)

						All supported constants:

						NaN, pi, inf, eps, phi, tau, g, e, c, gamma

						All supported functions:

						Normal:

						asinh, acosh, tan, atanh, atan2, cbrt, sign, floor, ceil, round, trunc, mod,
						gamma, factorial, integral, derivative

						Complex:

						mag, conjugate, minmag, maxmag, compgamma

						Normal and complex:

						sin, sinh, asin, cos, cosh, acos, tanh, atan,
						sind, cosd, tand, asind, acosd, atand,
						ln, log, log2, logb, exp, pow, sqrt,
						abs, min, max
						""",
					Category = "mathematics"
				});

			AddTool(Solve,
				new ToolInitializationInfo
				{
					Name = "math-solve",
					Description = """
						Solve an equation numerically for a given variable using the bisection method.
						Finds roots of f(x) = 0 within a search range.

						Examples:

						solve(x^2 - 4 = 0, x)
						solve(sin(x) - 0.5, x, -10, 10)
						solve(x^3 - 2*x - 5 = 0, x)

						The equation can be written as 'expression = 0' or just 'expression'.
						If no range is specified, it scans from -100 to 100 by default.
						""",
					Category = "mathematics"
				});
		}

		private ReactiveToolResult Calculate([Description("Expression to evaluate")] string expression)
		{
			try
			{
				var expressionEntity = MathExpressionParser.Parse(expression);
				var evalContext = new MathEvaluationContext();
				var resultEntity = expressionEntity.Evaluate(evalContext);
				var result = resultEntity.ToComplexOrThrow();
				var formatted = string.Empty;

				if (result.Imaginary != 0)
				{
					if (result.Imaginary > 0)
					{
						formatted = $"{result.Real} + {result.Imaginary}i";
					}
					else
					{
						formatted = $"{result.Real} - {-result.Imaginary}i";
					}
				}
				else
				{
					formatted = result.Real.ToString();
				}

				return new ReactiveToolResult
				{
					StatusIcon = Material.Icons.MaterialIconKind.Abacus,
					StatusTitle = $"`{expression}` = `{formatted}`",
					ResultContent = formatted
				}.CompleteWithSuccess();
			}
			catch (MathEvaluationException mex)
			{
				return new ReactiveToolResult
				{
					StatusIcon = Material.Icons.MaterialIconKind.Abacus,
					StatusTitle = $"`{expression}`",
					ResultContent = "Error evaluating expression: " + mex.Message
				}.CompleteWithError();
			}
			catch (ParsingException pex)
			{
				return new ReactiveToolResult
				{
					StatusIcon = Material.Icons.MaterialIconKind.Abacus,
					StatusTitle = $"`{expression}`",
					ResultContent = "Error parsing expression: " + pex.Message
				}.CompleteWithError();
			}
		}

		private ReactiveToolResult Solve(
			[Description("Equation to solve. Examples: 'x^2 - 4 = 0', 'sin(x) - 0.5', 'x^3 - 2*x - 5 = 0'")] string equation,
			[Description("Variable to solve for. Default: 'x'")] string variable = "x",
			[Description("Left bound of the search range. Default: -100")] double rangeStart = -100.0,
			[Description("Right bound of the search range. Default: 100")] double rangeEnd = 100.0,
			[Description("Step size for scanning. Smaller values find more roots but are slower. Default: 0.1")] double scanStep = 0.1)
		{
			try
			{
				// Parse the equation: support "expr = 0" or just "expr"
				string expressionStr = equation;
				if (equation.Contains('='))
				{
					var parts = equation.Split('=', 2);
					expressionStr = $"({parts[0].Trim()}) - ({parts[1].Trim()})";
				}

				var expression = MathExpressionParser.Parse(expressionStr);
				var roots = MathEquationSolver.FindRoots(expression, variable, rangeStart, rangeEnd, scanStep);

				if (roots.Length == 0)
				{
					return new ReactiveToolResult
					{
						StatusIcon = Material.Icons.MaterialIconKind.Abacus,
						StatusTitle = $"`{equation}`",
						ResultContent = "No roots found in the specified range."
					}.CompleteWithError();
				}

				var formatted = string.Join(", ", roots.Select(r => r.ToString("G")));
				return new ReactiveToolResult
				{
					StatusIcon = Material.Icons.MaterialIconKind.Abacus,
					StatusTitle = $"`{equation}`",
					ResultContent = $"Roots found for '{variable}': {formatted}"
				}.CompleteWithSuccess();
			}
			catch (MathEvaluationException mex)
			{
				return new ReactiveToolResult
				{
					StatusIcon = Material.Icons.MaterialIconKind.Abacus,
					StatusTitle = $"`{equation}`",
					ResultContent = "Error evaluating equation: " + mex.Message
				}.CompleteWithError();
			}
			catch (ParsingException pex)
			{
				return new ReactiveToolResult
				{
					StatusIcon = Material.Icons.MaterialIconKind.Abacus,
					StatusTitle = $"`{equation}`",
					ResultContent = "Error parsing equation: " + pex.Message
				}.CompleteWithError();
			}
		}
	}
}