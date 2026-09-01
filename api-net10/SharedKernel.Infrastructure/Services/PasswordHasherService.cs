using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using SharedKernel.Application.Interfaces;

namespace SharedKernel.Infrastructure.Services;

public class PasswordHasherService : IPasswordHasherService
{
    private const int IterationCount = 100_000;
    private const int SaltSize = 16;
    private const int SubkeySize = 32;

    public string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] subkey = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA512,
            iterationCount: IterationCount,
            numBytesRequested: SubkeySize);

        var outputBytes = new byte[1 + SaltSize + SubkeySize];
        outputBytes[0] = 0x01; // Format marker PBKDF2
        Buffer.BlockCopy(salt, 0, outputBytes, 1, SaltSize);
        Buffer.BlockCopy(subkey, 0, outputBytes, 1 + SaltSize, SubkeySize);

        return Convert.ToBase64String(outputBytes);
    }

    public (bool isValid, bool needsRehash) VerifyPassword(string password, string hashedPassword, string? saltCode, int hashType)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword))
            return (false, false);

        try
        {
            byte[] decoded = Convert.FromBase64String(hashedPassword);
            if (decoded.Length != 1 + SaltSize + SubkeySize || decoded[0] != 0x01)
                return (false, false);

            byte[] salt = new byte[SaltSize];
            Buffer.BlockCopy(decoded, 1, salt, 0, SaltSize);

            byte[] expectedSubkey = new byte[SubkeySize];
            Buffer.BlockCopy(decoded, 1 + SaltSize, expectedSubkey, 0, SubkeySize);

            byte[] actualSubkey = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA512,
                iterationCount: IterationCount,
                numBytesRequested: SubkeySize);

            return (CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey), false);
        }
        catch (FormatException)
        {
            return (false, false);
        }
    }
}
