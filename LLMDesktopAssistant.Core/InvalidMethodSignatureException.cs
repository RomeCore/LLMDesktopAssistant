using System.Reflection;

namespace LLMDesktopAssistant.Core
{
	/// <summary>
	/// Represents an exception that is thrown when a method signature is invalid.
	/// </summary>
	public class InvalidMethodSignatureException : Exception
	{
		/// <summary>
		/// Gets or sets the method that has an invalid signature.
		/// </summary>
		public MethodInfo Method { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="InvalidMethodSignatureException"/> class.
		/// </summary>
		/// <param name="method">The method that has an invalid signature.</param>
		/// <param name="parameterExplanation">A description of the invalid parameters.</param>
		public InvalidMethodSignatureException(MethodInfo method, string parameterExplanation)
			: base(FormatMessage(method, parameterExplanation))
		{
			Method = method;
		}

		private static string FormatMessage(MethodInfo method, string parameterExplanation)
		{
			return $"Method {method} has invalid signature: {parameterExplanation}";
		}
	}
}