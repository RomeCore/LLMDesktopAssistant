using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.WebUI
{
	public interface IPasswordHashingService
	{
		/// <summary>
		/// Hashes the given password using a secure hashing algorithm.
		/// </summary>
		/// <param name="password">The password to hash.</param>
		/// <returns>A hashed version of the password.</returns>
		string HashPassword(string password);

		/// <summary>
		/// Verifies that the provided password matches the given hashed password.
		/// </summary>
		/// <param name="hashedPassword">The hashed password to compare against.</param>
		/// <param name="plainPassword">The plain text password to verify.</param>
		/// <returns>True if the passwords match; otherwise, false.</returns>
		bool VerifyPassword(string hashedPassword, string plainPassword);
	}
}
