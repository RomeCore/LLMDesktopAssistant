using System.Numerics;

namespace LLMDesktopAssistant.Calculation.Ast
{
    /// <summary>
    /// Unary operations: plus, minus, factorial, absolute value.
    /// </summary>
    public enum UnaryOpKind
    {
        Plus,
        Minus,
        Factorial,
        Abs
    }

    /// <summary>
    /// Represents a unary operation applied to a sub-expression.
    /// </summary>
    public sealed class UnaryOpEntity : MathEntity
    {
        public UnaryOpKind Op { get; }
        public MathEntity Operand { get; }

        public UnaryOpEntity(UnaryOpKind op, MathEntity operand)
        {
            Op = op;
            Operand = operand;
        }

        public override MathEntity Evaluate(MathEvaluationContext ctx)
        {
            var operandVal = Operand.Evaluate(ctx);
            var value = operandVal.ToComplexOrThrow();

            Complex result = Op switch
            {
                UnaryOpKind.Plus => value,
                UnaryOpKind.Minus => -value,
                UnaryOpKind.Abs => Complex.Abs(value),
                UnaryOpKind.Factorial => ComputeFactorial((int)value.Real),
                _ => throw new MathEvaluationException($"Unknown unary operation: {Op}")
            };

            return new ConstantEntity(result);
        }

        private static Complex ComputeFactorial(int n)
        {
            if (n < 0)
            {
                return double.NaN;
            }
            if (n == 0)
            {
                return 1.0;
            }

            double result = 1.0;
            for (long i = 2; i <= n; i++)
            {
                result *= i;
                if (double.IsPositiveInfinity(result))
                {
                    break;
                }
            }
            return result;
        }

        public override string ToString()
        {
            return Op switch
            {
                UnaryOpKind.Plus => $"(+{Operand})",
                UnaryOpKind.Minus => $"(-{Operand})",
                UnaryOpKind.Factorial => $"({Operand}!)",
                UnaryOpKind.Abs => $"(|{Operand}|)",
                _ => $"(?{Operand})"
            };
        }
    }
}
