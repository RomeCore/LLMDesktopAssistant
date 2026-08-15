using System.Numerics;
using LLMDesktopAssistant.Calculation.Ast;

namespace LLMDesktopAssistant.Calculation
{
	/// <summary>
	/// Provides numerical differentiation methods for mathematical expressions.
	/// Currently uses central difference. Can be swapped for higher-order stencils, etc.
	/// </summary>
	public static class NumericalDifferentiation
	{
		/// <summary>
		/// Default step size for central difference.
		/// </summary>
		public const double DefaultStepSize = 1e-8;

		/// <summary>
		/// Computes the derivative of a mathematical expression at a given point
		/// using the central difference method.
		/// </summary>
		/// <param name="expression">The expression to differentiate (as a MathEntity tree).</param>
		/// <param name="variable">The variable to differentiate with respect to.</param>
		/// <param name="x0">The point at which to evaluate the derivative.</param>
		/// <param name="ctx">The evaluation context.</param>
		/// <param name="h">Step size. Default is <see cref="DefaultStepSize"/>.</param>
		/// <returns>The approximate value of the derivative at x0.</returns>
		public static double CentralDifference(
			MathEntity expression,
			string variable,
			double x0,
			MathEvaluationContext ctx,
			double h = DefaultStepSize)
		{
			double fp = EvaluateAt(expression, variable, x0 + h, ctx);
			double fm = EvaluateAt(expression, variable, x0 - h, ctx);
			return (fp - fm) / (2 * h);
		}

		/// <summary>
		/// Computes the derivative using a 5-point stencil for higher accuracy.
		/// </summary>
		public static double FivePointStencil(
			MathEntity expression,
			string variable,
			double x0,
			MathEvaluationContext ctx,
			double h = 1e-5)
		{
			double f2 = EvaluateAt(expression, variable, x0 + 2 * h, ctx);
			double f1 = EvaluateAt(expression, variable, x0 + h, ctx);
			double fm1 = EvaluateAt(expression, variable, x0 - h, ctx);
			double fm2 = EvaluateAt(expression, variable, x0 - 2 * h, ctx);
			return (f2 - 8 * f1 + 8 * fm1 - fm2) / (12 * h);
		}

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
