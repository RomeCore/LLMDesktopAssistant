namespace LLMDesktopAssistant.Calculation.Ast
{
    /// <summary>
    /// Represents a named variable in a mathematical expression.
    /// </summary>
    public sealed class VariableEntity : MathEntity
    {
        public string Name { get; }

        public VariableEntity(string name)
        {
            Name = name;
        }

        public override MathEntity Evaluate(MathEvaluationContext ctx)
        {
            if (ctx.TryGetVariable(Name, out var value))
            {
                return value;
            }
            throw new MathEvaluationException($"Unknown variable '{Name}'.");
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
