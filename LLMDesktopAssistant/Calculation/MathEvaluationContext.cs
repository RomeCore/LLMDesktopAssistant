using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;
using LLMDesktopAssistant.Calculation.Ast;

namespace LLMDesktopAssistant.Calculation
{
	/// <summary>
	/// Represents the context in which mathematical expressions are evaluated. It includes a dictionary of variables and their values.
	/// </summary>
	public class MathEvaluationContext
	{
		/// <summary>
		/// A dictionary containing all the variables and their values in the current context.
		/// </summary>
		public FrozenDictionary<string, MathEntity> Variables { get; }

		/// <summary>
		/// A stack to keep track of variable scopes. Each scope is represented as a dictionary of variables.
		/// </summary>
		public Stack<FrozenDictionary<string, MathEntity>> ScopeStack { get; } = [];

		/// <summary>
		/// Initializes a new instance of the <see cref="MathEvaluationContext"/> class with no initial variables.
		/// </summary>
		public MathEvaluationContext()
		{
			Variables = new Dictionary<string, MathEntity>().ToFrozenDictionary();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MathEvaluationContext"/> class with the specified variables.
		/// </summary>
		/// <param name="variables">The variables to initialize the context with.</param>
		public MathEvaluationContext(IDictionary<string, MathEntity> variables)
		{
			Variables = variables.ToFrozenDictionary();
		}

		/// <summary>
		/// Pushes a new scope onto the stack. The scope is a dictionary of variable names to their corresponding entities.
		/// </summary>
		/// <param name="scope">The new scope to push onto the stack.</param>
		public void PushScope(IDictionary<string, MathEntity> scope)
		{
			ScopeStack.Push(scope.ToFrozenDictionary());
		}

		/// <summary>
		/// Pops the current scope from the stack. This should only be called when there is at least one scope in the stack.
		/// </summary>
		public void PopScope()
		{
			ScopeStack.Pop();
		}

		/// <summary>
		/// Tries to get a variable by name. Searches through the scope stack from top to bottom.
		/// </summary>
		/// <param name="name">The name of the variable to get.</param>
		/// <param name="value">The variable if found.</param>
		/// <returns>True if the variable was found; otherwise, false.</returns>
		public bool TryGetVariable(string name, [NotNullWhen(true)] out MathEntity? value)
		{
			foreach (var scope in ScopeStack)
				if (scope.TryGetValue(name, out value))
					return true;
			value = null;
			return false;
		}
	}
}