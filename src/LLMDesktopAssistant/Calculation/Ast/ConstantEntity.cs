using System.Numerics;

namespace LLMDesktopAssistant.Calculation.Ast
{
	/// <summary>
	/// Represents a constant numeric value (real or complex).
	/// </summary>
	public class ConstantEntity : MathEntity
	{
		public Complex Value { get; }

		public ConstantEntity(Complex value)
		{
			Value = value;
		}

		public override MathEntity Evaluate(MathEvaluationContext ctx)
		{
			return this;
		}

		public override string ToString()
		{
			if (Value.Imaginary == 0)
			{
				return Value.Real.ToString();
			}
			if (Value.Imaginary > 0)
			{
				return $"{Value.Real} + {Value.Imaginary}i";
			}
			return $"{Value.Real} - {-Value.Imaginary}i";
		}
	}
}
