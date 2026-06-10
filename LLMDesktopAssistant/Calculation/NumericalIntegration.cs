using System.Numerics;
using LLMDesktopAssistant.Calculation.Ast;

namespace LLMDesktopAssistant.Calculation
{
	/// <summary>
	/// Provides numerical integration methods for mathematical expressions.
	/// Currently uses Simpson's rule. Can be swapped for adaptive quadrature, etc.
	/// </summary>
	public static class NumericalIntegration
	{
		/// <summary>
		/// Default number of steps for Simpson's rule.
		/// </summary>
		public const int DefaultSteps = 1000;

		/// <summary>
		/// Computes the definite integral of a mathematical expression using Simpson's rule.
		/// </summary>
		/// <param name="expression">The expression to integrate (as a MathEntity tree).</param>
		/// <param name="variable">The variable to integrate with respect to.</param>
		/// <param name="a">Lower bound of integration.</param>
		/// <param name="b">Upper bound of integration.</param>
		/// <param name="ctx">The evaluation context.</param>
		/// <param name="steps">Number of Simpson steps. Default is <see cref="DefaultSteps"/>.</param>
		/// <returns>The approximate value of the integral.</returns>
		public static double Simpson(
			MathEntity expression,
			string variable,
			double a,
			double b,
			MathEvaluationContext ctx,
			int steps = DefaultSteps)
		{
			double h = (b - a) / steps;
			double sum = 0;

			for (int i = 0; i < steps; i++)
			{
				double x0 = a + i * h;
				double x1 = x0 + h;
				double xm = (x0 + x1) / 2;

				double f0 = EvaluateAt(expression, variable, x0, ctx);
				double fm = EvaluateAt(expression, variable, xm, ctx);
				double f1 = EvaluateAt(expression, variable, x1, ctx);

				sum += h / 6 * (f0 + 4 * fm + f1);
			}

			return sum;
		}

		/// <summary>
		/// Evaluates an expression at a specific value of a variable.
		/// Temporarily pushes a scope with the variable set to the given value.
		/// </summary>
		private static double EvaluateAt(MathEntity expr, string var, double val, MathEvaluationContext ctx)
		{
			ctx.PushScope(new Dictionary<string, MathEntity>
			{
				{ var, new ConstantEntity(new Complex(val, 0)) }
			});
			try
			{
				return expr.Evaluate(ctx).ToComplexOrThrow().Real;
			}
			finally
			{
				ctx.PopScope();
			}
		}
	}
}
