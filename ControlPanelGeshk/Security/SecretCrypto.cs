using System.Security.Cryptography;
using System.Text;

namespace ControlPanelGeshk.Security;

/// Cifra/descifra secretos con AES-GCM.
/// Clave: se deriva de Encryption__MasterKey (cualquier string) → SHA-256 (32 bytes).
public interface ISecretCrypto
{
    string Encrypt(string plaintext);
    string Decrypt(string encrypted); // base64(nonce|cipher|tag)
}

public class AesGcmSecretCrypto : ISecretCrypto
{
    private readonly byte[] _key;

    public AesGcmSecretCrypto(IConfiguration cfg)
    {
        var raw = cfg["Encryption:MasterKey"] ?? cfg["Encryption__MasterKey"]
            ?? throw new InvalidOperationException("Falta Encryption__MasterKey en el entorno/.env.");
        // Derivar a 32 bytes de forma estable (evita exigir Base64 exacto)
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
    }

    public string Encrypt(string plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plain = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[plain.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(_key);
        aes.Encrypt(nonce, plain, cipher, tag);

        var payload = new byte[nonce.Length + cipher.Length + tag.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
        Buffer.BlockCopy(cipher, 0, payload, nonce.Length, cipher.Length);
        Buffer.BlockCopy(tag, 0, payload, nonce.Length + cipher.Length, tag.Length);

        return Convert.ToBase64String(payload);
    }

    public string Decrypt(string encrypted)
    {
        var payload = Convert.FromBase64String(encrypted);
        var nonce = new byte[12];
        var tag = new byte[16];
        var cipher = new byte[payload.Length - nonce.Length - tag.Length];

        Buffer.BlockCopy(payload, 0, nonce, 0, nonce.Length);
        Buffer.BlockCopy(payload, nonce.Length, cipher, 0, cipher.Length);
        Buffer.BlockCopy(payload, nonce.Length + cipher.Length, tag, 0, tag.Length);

        var plain = new byte[cipher.Length];
        using var aes = new AesGcm(_key);
        aes.Decrypt(nonce, cipher, tag, plain);

        return Encoding.UTF8.GetString(plain);
    }
}
