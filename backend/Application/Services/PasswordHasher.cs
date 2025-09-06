using System.Security.Cryptography;

namespace TPFinal.Api.Application;

/// <summary>
/// Utilidad para hasheo y verificación de contraseñas.
/// </summary>
/// <remarks>
/// Implementa PBKDF2 con SHA-256, sal aleatoria y un número fijo de iteraciones.
/// </remarks>
public static class PasswordHasher
{
    private const int SaltSize = 16;      // 128 bits
    private const int KeySize  = 32;      // 256 bits
    private const int Iterations = 100_000; // PBKDF2 iterations

    /// <summary>
    /// Genera el hash y la sal para una contraseña.
    /// </summary>
    /// <remarks>
    /// Utiliza PBKDF2 con SHA-256, una sal aleatoria y un número fijo de iteraciones.
    /// </remarks>
    public static (byte[] hash, byte[] salt) Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return (hash, salt);
    }

    /// <summary>
    /// Verifica una contraseña contra un hash y sal existentes.
    /// </summary>
    /// <remarks>
    /// Recalcula el hash con la sal y compara de forma segura.
    /// </remarks>
    public static bool Verify(string password, byte[] hash, byte[] salt)
    {
        var testHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return CryptographicOperations.FixedTimeEquals(testHash, hash);
    }
}
