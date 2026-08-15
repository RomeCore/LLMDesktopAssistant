using System.Numerics;

namespace LLMDesktopAssistant.Calculation.Ast
{
    /// <summary>
    /// Predefined named constants like pi, e, infinity, etc.
    /// </summary>
    public enum NamedConstantKind
    {
        NaN,
        Pi,
        E,
        Phi,
        Tau,
        Gamma,
        Infinity,
        Epsilon,
        G,  // gravitational acceleration 9.81
        C   // speed of light 299792458
    }

    /// <summary>
    /// Represents a predefined named constant. Inherits from <see cref="ConstantEntity"/>
    /// since all named constants resolve to a fixed numeric value.
    /// </summary>
    public sealed class NamedConstantEntity : ConstantEntity
    {
        public NamedConstantKind Kind { get; }

        private static Complex GetValueForKind(NamedConstantKind kind)
        {
            return kind switch
            {
                NamedConstantKind.NaN => double.NaN,
                NamedConstantKind.Pi => Math.PI,
                NamedConstantKind.E => double.E,
                NamedConstantKind.Phi => 1.618033988749895,
                NamedConstantKind.Tau => 6.283185307179586,
                NamedConstantKind.Gamma => 0.5772156649015328,
                NamedConstantKind.Infinity => double.PositiveInfinity,
                NamedConstantKind.Epsilon => double.Epsilon,
                NamedConstantKind.G => 9.81,
                NamedConstantKind.C => 299792458.0,
                _ => throw new MathEvaluationException($"Unknown named constant kind: {kind}")
            };
        }

        public NamedConstantEntity(NamedConstantKind kind)
            : base(GetValueForKind(kind))
        {
            Kind = kind;
        }

        public override string ToString()
        {
            return Kind switch
            {
                NamedConstantKind.NaN => "NaN",
                NamedConstantKind.Pi => "pi",
                NamedConstantKind.E => "e",
                NamedConstantKind.Phi => "phi",
                NamedConstantKind.Tau => "tau",
                NamedConstantKind.Gamma => "gamma",
                NamedConstantKind.Infinity => "inf",
                NamedConstantKind.Epsilon => "eps",
                NamedConstantKind.G => "g",
                NamedConstantKind.C => "c",
                _ => "?"
            };
        }
    }
}
