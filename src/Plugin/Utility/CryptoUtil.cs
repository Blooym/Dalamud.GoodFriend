using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using GoodFriend.Plugin.Base;

namespace GoodFriend.Plugin.Utility;

internal static class CryptoUtil
{
    /// <summary>
    ///     Gets the module version identifier of the current assembly.
    /// </summary>
    /// <remarks>
    ///    This is used as a salt for hashing values as it allows only users on the same build to
    ///    read the hashed values unless this value is extracted manually.
    /// </remarks>
    private static Guid GetModuleVersionId() => Assembly.GetExecutingAssembly().ManifestModule.ModuleVersionId;

    /// <summary>
    ///     Generates a random 16 byte salt.
    /// </summary>
    public static byte[] GenerateSalt()
    {
        using var seed = RandomNumberGenerator.Create();
        var bytes = new byte[16];
        seed.GetBytes(bytes);
        return bytes;
    }

    /// <summary>
    ///     Creates a 32 byte HMACSHA256 hash from the given value and salt.
    ///     This also uses the user's configured PrivateGroupKey as the HMAC key if one is set,
    ///     as well as other build-time constants to ensure the hash is unique.
    /// </summary>
    /// <param name="value">The value to hash.</param>
    /// <param name="salt">The salt to hash with. Recommended to use the <see cref="GenerateSalt"/> method for this.</param>
    public static byte[] HashValueWithSalt(object value, byte[] salt)
    {
        var dataBytes = Encoding.UTF8.GetBytes($"{value}:{Convert.ToBase64String(salt)}:{DateTime.UtcNow:yyyyMMddHH}:{GetModuleVersionId()}");
        var keyBytes = Encoding.UTF8.GetBytes(Services.PluginConfiguration.ApiConfig.PrivateGroupKey);
        using var hmac = new HMACSHA256(keyBytes);
        return hmac.ComputeHash(dataBytes);
    }
}
