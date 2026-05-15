using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Settings;
using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.WebUI
{
	/// <summary>
	/// Provides methods for managing user information.
	/// </summary>
	public interface IUserManagementService
	{
		/// <summary>
		/// Lists all users. Returns an empty collection if no users are found.
		/// </summary>
		/// <returns>A collection of user information.</returns>
		IEnumerable<UserInformation> GetAllUsers();

		/// <summary>
		/// Lists all local users. Returns an empty collection if no local users are found.
		/// </summary>
		/// <returns>A collection of local user information.</returns>
		IEnumerable<UserInformation> GetLocalUsers();

		/// <summary>
		/// Lists all remote users that have connected via WebUI. Returns an empty collection if no remote users are found.
		/// </summary>
		/// <returns>A collection of remote user information.</returns>
		IEnumerable<UserInformation> GetRemoteUsers();

		/// <summary>
		/// Determines whether the specified user is a local user that have been defined in chat settings.
		/// </summary>
		/// <param name="userLogin">The user login to check.</param>
		/// <returns>true if the specified user is a local user; otherwise, false.</returns>
		bool IsLocalUser(string userLogin);

		/// <summary>
		/// Lists all active users. Returns an empty collection if no active users are found.
		/// User is active when 
		/// </summary>
		/// <returns></returns>
		IEnumerable<UserInformation> GetActiveUsers();

		/// <summary>
		/// Finds a user by their login. Returns null if no such user exists.
		/// </summary>
		/// <param name="login">The login of the user to find.</param>
		/// <returns>The user information if found; otherwise, null.</returns>
		UserInformation? FindByLogin(string login);

		/// <summary>
		/// Registers a new user with the specified login and password. If the user already exists, returns null.
		/// </summary>
		/// <param name="login">The login for the new user.</param>
		/// <param name="password">The password for the new user.</param>
		/// <param name="name">The name of the new user (optional).</param>
		/// <param name="description">The description of the new user (optional).</param>
		/// <returns>The new user information if registration is successful; otherwise, null.</returns>
		UserInformation? RegisterUser(string login, string password, string? name, string? description);
	}
}