using System.Numerics;

namespace LLMDesktopAssistant.Calculation.Ast
{
    /// <summary>
    /// Represents the imaginary unit 'i' (sqrt(-1)).
    /// </summary>
    public sealed class ImaginaryEntity : ConstantEntity
    {
        public ImaginaryEntity()
            : base(new Complex(0, 1))
        {
        }

        public override string ToString()
        {
            return "i";
        }
    }
}
