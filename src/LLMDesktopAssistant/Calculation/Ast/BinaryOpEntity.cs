using System.Numerics;

namespace LLMDesktopAssistant.Calculation.Ast
{
    /// <summary>
    /// Binary arithmetic operations.
    /// </summary>
    public enum BinaryOpKind
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Power,
        Mod
    }

    /// <summary>
    /// Represents a binary operation between two sub-expressions.
    /// </summary>
    public sealed class BinaryOpEntity : MathEntity
    {
        public BinaryOpKind Op { get; }
        public MathEntity Left { get; }
        public MathEntity Right { get; }

        public BinaryOpEntity(BinaryOpKind op, MathEntity left, MathEntity right)
        {
            Op = op;
            Left = left;
            Right = right;
        }

        public override MathEntity Evaluate(MathEvaluationContext ctx)
        {
            var leftVal = Left.Evaluate(ctx);
            var rightVal = Right.Evaluate(ctx);

            var leftComplex = leftVal.ToComplexOrThrow();
            var rightComplex = rightVal.ToComplexOrThrow();

            Complex result = Op switch
            {
                BinaryOpKind.Add => leftComplex + rightComplex,
                BinaryOpKind.Subtract => leftComplex - rightComplex,
                BinaryOpKind.Multiply => leftComplex * rightComplex,
                BinaryOpKind.Divide => leftComplex / rightComplex,
                BinaryOpKind.Power => Complex.Pow(leftComplex, rightComplex),
                BinaryOpKind.Mod => new Complex(leftComplex.Real % rightComplex.Real, 0),
                _ => throw new MathEvaluationException($"Unknown binary operation: {Op}")
            };

            return new ConstantEntity(result);
        }

        public override string ToString()
        {
            string opStr = Op switch
            {
                BinaryOpKind.Add => "+",
                BinaryOpKind.Subtract => "-",
                BinaryOpKind.Multiply => "*",
                BinaryOpKind.Divide => "/",
                BinaryOpKind.Power => "^",
                BinaryOpKind.Mod => "%",
                _ => "?"
            };
            return $"({Left} {opStr} {Right})";
        }
    }
}
