using LLMDesktopAssistant.Services;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace LLMDesktopAssistant.Users
{
	[Service(typeof(IPasswordHashingService))]
	public class PasswordHashingService : IPasswordHashingService
	{
		private const int SaltSize = 128 / 8; // 16 bytes
		private const int HashSize = 256 / 8; // 32 bytes
		private const int Iterations = 10000;

		public string HashPassword(string password)
		{
			if (string.IsNullOrEmpty(password))
				throw new ArgumentException("Password cannot be null or empty");

			byte[] salt = new byte[SaltSize];
			using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
			{
				rng.GetBytes(salt);
			}

			byte[] hash = KeyDerivation.Pbkdf2(
				password: password,
				salt: salt,
				prf: KeyDerivationPrf.HMACSHA256,
				iterationCount: Iterations,
				numBytesRequested: HashSize
			);

			string saltBase64 = Convert.ToBase64String(salt);
			string hashBase64 = Convert.ToBase64String(hash);

			return $"{Iterations}:{saltBase64}:{hashBase64}";
		}

		public bool VerifyPassword(string hashedPassword, string plainPassword)
		{
			if (string.IsNullOrEmpty(hashedPassword) || string.IsNullOrEmpty(plainPassword))
				return false;

			var parts = hashedPassword.Split(':');
			if (parts.Length != 3)
				return false;

			int iterations = int.Parse(parts[0]);
			byte[] salt = Convert.FromBase64String(parts[1]);
			byte[] expectedHash = Convert.FromBase64String(parts[2]);

			byte[] actualHash = KeyDerivation.Pbkdf2(
				password: plainPassword,
				salt: salt,
				prf: KeyDerivationPrf.HMACSHA256,
				iterationCount: iterations,
				numBytesRequested: expectedHash.Length
			);

			return CryptographicCompare(expectedHash, actualHash);
		}

		private bool CryptographicCompare(byte[] a, byte[] b)
		{
			if (a.Length != b.Length)
				return false;

			int result = 0;
			for (int i = 0; i < a.Length; i++)
			{
				result |= a[i] ^ b[i];
			}

			return result == 0;
		}
	}
}
