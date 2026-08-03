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
        /// Verifies a plain-text password against a BCrypt hash.
        /// </summary>
        public static bool Verify(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}
