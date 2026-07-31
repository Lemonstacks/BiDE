namespace BiDE.Helpers
{
    public static class PasswordHasher
    {
        /// <summary>
        /// Hashes a plain-text password using BCrypt with a work factor of 12.
        /// </summary>
        public static string Hash(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        /// <summary>
        /// Verifies a plain-text password against a stored password.
        /// Handles both BCrypt hashes and legacy plain-text passwords.
        /// </summary>
        public static bool Verify(string password, string storedPassword)
        {
            if (IsBCryptHash(storedPassword))
            {
                return BCrypt.Net.BCrypt.Verify(password, storedPassword);
            }

            // Legacy plain-text comparison
            return password == storedPassword;
        }

        /// <summary>
        /// Checks whether a stored password is a BCrypt hash (starts with $2).
        /// </summary>
        public static bool IsBCryptHash(string storedPassword)
        {
            return !string.IsNullOrEmpty(storedPassword) && storedPassword.StartsWith("$2");
        }
    }
}
