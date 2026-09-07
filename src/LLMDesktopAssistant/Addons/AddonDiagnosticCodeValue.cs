using System.Diagnostics.CodeAnalysis;

namespace LLMDesktopAssistant.Addons
{
	public readonly struct AddonDiagnosticCodeValue : IEquatable<AddonDiagnosticCodeValue>
	{
		public ulong Value { get; }

		public AddonDiagnosticCodeValue(ulong value)
		{
			Value = value;
		}

		public static implicit operator AddonDiagnosticCodeValue(ulong value)
		{
			return new AddonDiagnosticCodeValue(value);
		}

		public static implicit operator ulong(AddonDiagnosticCodeValue value)
		{
			return value.Value;
		}

		public static implicit operator AddonDiagnosticCodeValue(int value)
		{
			return new AddonDiagnosticCodeValue((ulong)value);
		}

		public static implicit operator int(AddonDiagnosticCodeValue value)
		{
			return (int)value.Value;
		}

		public static bool operator ==(AddonDiagnosticCodeValue left, AddonDiagnosticCodeValue right)
		{
			return left.Value == right.Value;
		}

		public static bool operator !=(AddonDiagnosticCodeValue left, AddonDiagnosticCodeValue right)
		{
			return left.Value != right.Value;
		}

		public static bool operator >(AddonDiagnosticCodeValue left, AddonDiagnosticCodeValue right)
		{
			return left.Value > right.Value;
		}

		public static bool operator <(AddonDiagnosticCodeValue left, AddonDiagnosticCodeValue right)
		{
			return left.Value < right.Value;
		}

		public static bool operator >=(AddonDiagnosticCodeValue left, AddonDiagnosticCodeValue right)
		{
			return left.Value >= right.Value;
		}

		public static bool operator <=(AddonDiagnosticCodeValue left, AddonDiagnosticCodeValue right)
		{
			return left.Value <= right.Value;
		}

		public static AddonDiagnosticCodeValue operator +(AddonDiagnosticCodeValue left, AddonDiagnosticCodeValue right)
		{
			return new AddonDiagnosticCodeValue(left.Value + right.Value);
		}

		public static AddonDiagnosticCodeValue operator -(AddonDiagnosticCodeValue left, AddonDiagnosticCodeValue right)
		{
			return new AddonDiagnosticCodeValue(left.Value - right.Value);
		}

		public static AddonDiagnosticCodeValue operator *(AddonDiagnosticCodeValue left, AddonDiagnosticCodeValue right)
		{
			return new AddonDiagnosticCodeValue(left.Value * right.Value);
		}

		public static AddonDiagnosticCodeValue operator /(AddonDiagnosticCodeValue left, AddonDiagnosticCodeValue right)
		{
			return new AddonDiagnosticCodeValue(left.Value / right.Value);
		}

		public static AddonDiagnosticCodeValue operator ~(AddonDiagnosticCodeValue value)
		{
			return new AddonDiagnosticCodeValue(~value.Value);
		}

		public static AddonDiagnosticCodeValue operator |(AddonDiagnosticCodeValue left, AddonDiagnosticCodeValue right)
		{
			return new AddonDiagnosticCodeValue(left.Value | right.Value);
		}

		public static AddonDiagnosticCodeValue operator &(AddonDiagnosticCodeValue left, AddonDiagnosticCodeValue right)
		{
			return new AddonDiagnosticCodeValue(left.Value & right.Value);
		}

		public static AddonDiagnosticCodeValue operator ^(AddonDiagnosticCodeValue left, AddonDiagnosticCodeValue right)
		{
			return new AddonDiagnosticCodeValue(left.Value ^ right.Value);
		}

		public readonly bool Has(AddonDiagnosticCodeValue code)
		{
			return (Value & code.Value) == code.Value;
		}

		public readonly bool HasAny(AddonDiagnosticCodeValue code)
		{
			return (Value & code.Value) != 0;
		}

		public readonly bool Equals(AddonDiagnosticCodeValue other)
		{
			return Value == other.Value;
		}

		public override readonly bool Equals([NotNullWhen(true)] object? obj)
		{
			return obj is AddonDiagnosticCodeValue other && Equals(other);
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}
	}
}
