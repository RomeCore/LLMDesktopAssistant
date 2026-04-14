using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using LLMDesktopAssistant.Core.Services;
using RCLargeLanguageModels.Tools;
using RCParsing;
using LLMDesktopAssistant.Core.Calculation;

namespace LLMDesktopAssistant.Core.ToolModules
{
	[Service]
	public class CalculationToolModule : ToolModule
	{
		public CalculationToolModule()
		{
			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(Calculate, "calculate", """
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
				"""),
				Category = "calculation"
			});
		}

		private ToolResult Calculate([Description("Expression to evaluate")] string expression)
		{
			try
			{
				var result = Calculator.ParseValue(expression);
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

				return new ToolResult(formatted);
			}
			catch (ParsingException pex)
			{
				return new ToolResult(ToolResultStatus.Error, pex.Message);
			}
		}
	}
}