namespace LLMDesktopAssistant.Utils
{
	/// <summary>
	/// A string comparer that ignores case for identifiers.
	/// Ignores any characters that are not unicode letters or digits.
	/// </summary>
	public class IdentifierIgnoreCaseComparer : StringComparer
	{
		public static IdentifierIgnoreCaseComparer Instance { get; } = new IdentifierIgnoreCaseComparer();

		private static ReadOnlySpan<char> Transform(string? input)
		{
			var inputSpan = input.AsSpan();
			if (inputSpan.IsEmpty)
				return [];

			var result = new char[inputSpan.Length];
			int resultIndex = 0;
			for (int i = 0; i < inputSpan.Length; i++)
			{
				char c = inputSpan[i];
				if (char.IsLetterOrDigit(c))
					result[resultIndex++] = c;
			}

			return result.AsSpan(0, resultIndex);
		}

		public override int Compare(string? x, string? y)
		{
			var _x = Transform(x);
			var _y = Transform(y);
			return _x.CompareTo(_y, StringComparison.OrdinalIgnoreCase);
		}

		public override bool Equals(string? x, string? y)
		{
			var _x = Transform(x);
			var _y = Transform(y);
			return _x.Equals(_y, StringComparison.OrdinalIgnoreCase);
		}

		public override int GetHashCode(string obj)
		{
			var _obj = Transform(obj).ToString();
			return OrdinalIgnoreCase.GetHashCode(_obj);
		}
	}
}
