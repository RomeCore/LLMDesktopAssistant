using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Tools.Specifiers
{
	public enum SpecifierBehaviourUnionMode
	{
		/// <summary>
		/// Specifiers are disabled for tool.
		/// </summary>
		Disabled,

		/// <summary>
		/// Specifier verdict behaviours and tool preview behaviours are combined using a logical OR. <br/>
		/// The combination matrix is as follows:
		/// <code>
		/// V_spec \ V_policy | Approve  | Ask      | Disallow
		/// ------------------+----------+----------+---------
		/// Approve           | Approve  | Ask      | Disallow
		/// Ask               | Ask      | Ask      | Disallow
		/// Disallow          | Disallow | Disallow | Disallow
		/// </code>
		/// </summary>
		CombineHard,

		/// <summary>
		/// Specifier verdict behaviours and tool preview behaviours are combined using a logical OR,
		/// but tool's approve behaviours is replaced with ~Disallow behaviours. This results in a more
		/// specifier's stronger behaviour. <br/>
		/// The combination matrix is as follows:
		/// <code>
		/// V_spec \ V_policy | Approve  | Ask      | Disallow
		/// ------------------+----------+----------+---------
		/// Approve           | Approve  | Approve  | Disallow
		/// Ask               | Ask      | Ask      | Disallow
		/// Disallow          | Disallow | Disallow | Disallow
		/// </code>
		/// </summary>
		CombineSoft,

		/// <summary>
		/// Tool's preview behaviours are ignored, and only the specifier's verdict is used. <br/>
		/// The combination matrix is as follows:
		/// <code>
		/// V_spec \ V_policy | Approve  | Ask      | Disallow
		/// ------------------+----------+----------+---------
		/// Approve           | Approve  | Approve  | Approve
		/// Ask               | Ask      | Ask      | Ask
		/// Disallow          | Disallow | Disallow | Disallow
		/// </code>
		/// </summary>
		IgnoreNonSpecifierBehaviours
	}
}
