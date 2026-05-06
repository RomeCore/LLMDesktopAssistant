using LLMDesktopAssistant.LLM.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.LLM.Services.Users
{
	/// <summary>
	/// Provides methods for managing user information.
	/// </summary>
	public interface IUserManagementService
	{
		/// <summary>
		/// Finds a user by their login. Returns null if no such user exists.
		/// </summary>
		/// <param name="login">The login of the user to find.</param>
		/// <returns>The user information if found; otherwise, null.</returns>
		UserInformation? FindByLogin(string login);

		/// <summary>
		/// Lists all users. Returns an empty collection if no users are found.
		/// </summary>
		/// <returns>A collection of user information.</returns>
		IEnumerable<UserInformation> GetAllUsers();
	}
}