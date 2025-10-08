using System.Security.Cryptography;

namespace Demo.Application.Utils;

/// <summary>
/// AES 암호화/복호화 헬퍼 클래스
/// </summary>
public static class AesHelper
{
    /// <summary>
    /// AES Key/IV 생성 (256비트 키, 128비트 IV)
    /// </summary>
    public static (byte[] Key, byte[] IV) GenerateKeyAndIV()
    {
        using var aes = Aes.Create();
        aes.KeySize = 256;  // 256비트 키
        aes.GenerateKey();
        aes.GenerateIV();
        return (aes.Key, aes.IV);
    }

    /// <summary>
    /// 데이터 암호화
    /// </summary>
    public static byte[] Encrypt(ReadOnlySpan<byte> data, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        return encryptor.TransformFinalBlock(data.ToArray(), 0, data.Length);
    }

    /// <summary>
    /// 데이터 복호화
    /// </summary>
    public static byte[] Decrypt(ReadOnlySpan<byte> encryptedData, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(encryptedData.ToArray(), 0, encryptedData.Length);
    }
}