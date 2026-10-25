using System.Security.Cryptography;
using System.Text;
using NovaShop.Data;

namespace NovaShop.Services;

/// <summary>Versioned password hashing. Legacy demo hashes are accepted once and upgraded after a successful login.</summary>
public static class PasswordSecurity
{
    private const int Iterations = 210_000;
    private const int SaltLength = 16;
    private const int KeyLength = 32;
    private const string Prefix = "PBKDF2-SHA256";

    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeyLength);
        return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string storedHash, string password, out bool needsUpgrade)
    {
        needsUpgrade = false;
        if (storedHash.StartsWith(Prefix + "$", StringComparison.Ordinal))
        {
            var parts = storedHash.Split('$');
            if (parts.Length != 4 || !int.TryParse(parts[1], out var iterations) || iterations < 100_000 || iterations > 2_000_000)
                return false;
            try
            {
                var salt = Convert.FromBase64String(parts[2]);
                var expected = Convert.FromBase64String(parts[3]);
                if (salt.Length < 16 || expected.Length != KeyLength) return false;
                var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
                needsUpgrade = iterations < Iterations;
                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }
            catch (FormatException) { return false; }
        }

        // Compatibility for databases created by the earlier BTVH/NovaShop demo.
        var legacy = Encoding.UTF8.GetBytes(DbInitializer.Hash(password));
        var stored = Encoding.UTF8.GetBytes(storedHash ?? "");
        var valid = legacy.Length == stored.Length && CryptographicOperations.FixedTimeEquals(legacy, stored);
        needsUpgrade = valid;
        return valid;
    }
}
