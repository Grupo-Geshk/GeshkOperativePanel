using Konscious.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;

namespace ControlPanelGeshk.Security;

public class Argon2PasswordHasher : IPasswordHasher
{
    private readonly int _iterations = 4;
    private readonly int _memoryKb = 1024 * 64; // 64 MB
    private readonly int _degreeOfParallelism = 2;
    private readonly int _saltSize = 16;
    private readonly int _hashSize = 32;

    public string Hash(string password)
    {
        // generar salt aleatorio
        var salt = RandomNumberGenerator.GetBytes(_saltSize);

        // derivar key con Argon2id
        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Iterations = _iterations,
            MemorySize = _memoryKb,
            DegreeOfParallelism = _degreeOfParallelism,
            Salt = salt
        };

        var hashBytes = argon2.GetBytes(_hashSize);

        // concatenar salt+hash y pasar a Base64
        var combined = new byte[_saltSize + _hashSize];
        Buffer.BlockCopy(salt, 0, combined, 0, _saltSize);
        Buffer.BlockCopy(hashBytes, 0, combined, _saltSize, _hashSize);

        return Convert.ToBase64String(combined);
    }

    public bool Verify(string password, string hash)
    {
        var combined = Convert.FromBase64String(hash);

        // separar salt y hash original
        var salt = new byte[_saltSize];
        var originalHash = new byte[_hashSize];
        Buffer.BlockCopy(combined, 0, salt, 0, _saltSize);
        Buffer.BlockCopy(combined, _saltSize, originalHash, 0, _hashSize);

        // derivar hash con la misma config
        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Iterations = _iterations,
            MemorySize = _memoryKb,
            DegreeOfParallelism = _degreeOfParallelism,
            Salt = salt
        };
        var testHash = argon2.GetBytes(_hashSize);

        // comparar
        return CryptographicOperations.FixedTimeEquals(originalHash, testHash);
    }
}
