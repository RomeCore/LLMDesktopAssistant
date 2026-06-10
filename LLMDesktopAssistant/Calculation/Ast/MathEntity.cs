using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Calculation.Ast
{
	/// <summary>
	/// Represents a mathematical entity that can be evaluated.
	/// </summary>
	public abstract class MathEntity
	{
		/// <summary>
		/// Evaluates the mathematical expression represented by this entity.
		/// </summary>
		/// <param name="ctx">The context in which the evaluation occurs. This can include variables, constants, and other entities.</param>
		/// <returns>The result of evaluating the expression as a new entity.</returns>
		public abstract MathEntity Evaluate(MathEvaluationContext ctx);
	}
}
