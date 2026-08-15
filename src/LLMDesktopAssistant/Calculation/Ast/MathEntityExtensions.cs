using System.Numerics;

namespace LLMDesktopAssistant.Calculation.Ast
{
    /// <summary>
    /// Extension methods for <see cref="MathEntity"/> to simplify numeric conversions.
    /// </summary>
    public static class MathEntityExtensions
    {
        /// <summary>
        /// Attempts to extract a <see cref="Complex"/> value from a <see cref="MathEntity"/>.
        /// Supports <see cref="ConstantEntity"/> and its subclasses.
        /// </summary>
        /// <param name="entity">The entity to convert.</param>
        /// <param name="value">The extracted complex value, if successful.</param>
        /// <returns>True if conversion succeeded; otherwise false.</returns>
        public static bool TryToComplex(this MathEntity entity, out Complex value)
        {
            if (entity is ConstantEntity constant)
            {
                value = constant.Value;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Extracts a <see cref="Complex"/> value from a <see cref="MathEntity"/>.
        /// Throws <see cref="MathEvaluationException"/> if conversion fails.
        /// </summary>
        /// <param name="entity">The entity to convert.</param>
        /// <returns>The complex value.</returns>
        public static Complex ToComplexOrThrow(this MathEntity entity)
        {
            if (entity.TryToComplex(out var value))
            {
                return value;
            }
            throw new MathEvaluationException(
                $"Cannot convert entity of type '{entity.GetType().Name}' to a numeric value.");
        }
    }
}
