// Backend purpose: Refuses to start with missing or known-leaked secrets, so a deployment cannot
// silently run with the signing key or connection string that used to be committed to git.
using System.Security.Cryptography;
using System.Text;

namespace API;

public static class ConfigurationSafety
{
    public const string JwtKeyPath = "Jwt:SecretKey";
    public const string ConnectionStringName = "DefaultConnection";

    // HS256 needs at least 256 bits of key material.
    public const int MinimumJwtKeyBytes = 32;

    // Keys that have been published (in git history). Never accepted, not even in Development.
    private static readonly string[] RevokedJwtKeys =
    [
        "SuperHemligOchLångNyckelSomIngenVet123!"
    ];

    /// <summary>
    /// Checks the JWT signing key. In Development a missing key is replaced by a random key held only
    /// in memory (tokens stop working on restart); everywhere else a missing key stops startup.
    /// Returns true when a development key was generated.
    /// </summary>
    public static bool EnsureJwtKey(IConfiguration configuration, bool development)
    {
        var key = configuration[JwtKeyPath];

        if (string.IsNullOrWhiteSpace(key))
        {
            if (!development)
                throw new InvalidOperationException(
                    $"{JwtKeyPath} saknas. Sätt den via miljövariabeln Jwt__SecretKey eller en nyckelhanterare (t.ex. Azure Key Vault).");

            configuration[JwtKeyPath] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            return true;
        }

        if (RevokedJwtKeys.Contains(key))
            throw new InvalidOperationException(
                $"{JwtKeyPath} är en nyckel som har publicerats i git och får inte användas. Generera en ny.");

        if (Encoding.UTF8.GetByteCount(key) < MinimumJwtKeyBytes)
            throw new InvalidOperationException(
                $"{JwtKeyPath} är för kort. Den måste vara minst {MinimumJwtKeyBytes} byte.");

        return false;
    }

    /// <summary>Outside Development the connection string must come from the environment, not from a default.</summary>
    public static void RequireConnectionString(IConfiguration configuration, bool development)
    {
        if (!development && string.IsNullOrWhiteSpace(configuration.GetConnectionString(ConnectionStringName)))
            throw new InvalidOperationException(
                $"ConnectionStrings:{ConnectionStringName} saknas. Sätt den via miljövariabeln ConnectionStrings__{ConnectionStringName}.");
    }
}
