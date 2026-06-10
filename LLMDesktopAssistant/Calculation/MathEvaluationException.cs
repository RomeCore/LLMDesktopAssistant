using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Calculation
{
	public class MathEvaluationException : Exception
	{
		public MathEvaluationException(string message) : base(message)
		{
		}

		public MathEvaluationException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}