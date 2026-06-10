using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Calculation.Ast
{
	/// <summary>
	/// Represents an universal mathematical entity that can be created from C# function.
	/// </summary>
	public class LambdaEntity : MathEntity
	{
		public Func<MathEvaluationContext, MathEntity> Lambda { get; }

		public LambdaEntity(Func<MathEvaluationContext, MathEntity> lambda)
		{
			Lambda = lambda;
		}

		public override MathEntity Evaluate(MathEvaluationContext ctx)
		{
			return Lambda(ctx);
		}

		public override string ToString()
		{
			return "$csharp_lambda$";
		}
	}
}