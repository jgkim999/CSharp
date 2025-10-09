using System.Buffers;
using System.Security.Cryptography;

namespace Demo.Application.Utils;

/// <summary>
/// AES 암호화/복호화 헬퍼 클래스
/// ArrayPool을 사용하여 메모리 할당 최소화
/// </summary>
public static class AesHelper
{
    private static readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;

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
    /// 데이터 암호화 (기존 메서드 - 호환성 유지)
    /// </summary>
    [Obsolete("Use EncryptToPool for better performance")]
    public static byte[] Encrypt(ReadOnlySpan<byte> data, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();

        // ToArray() 호출 최소화
        var inputBuffer = _arrayPool.Rent(data.Length);
        try
        {
            data.CopyTo(inputBuffer);
            return encryptor.TransformFinalBlock(inputBuffer, 0, data.Length);
        }
        finally
        {
            _arrayPool.Return(inputBuffer);
        }
    }

    /// <summary>
    /// 데이터 복호화 (기존 메서드 - 호환성 유지)
    /// </summary>
    [Obsolete("Use DecryptToPool for better performance")]
    public static byte[] Decrypt(ReadOnlySpan<byte> encryptedData, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();

        // ToArray() 호출 최소화
        var inputBuffer = _arrayPool.Rent(encryptedData.Length);
        try
        {
            encryptedData.CopyTo(inputBuffer);
            return decryptor.TransformFinalBlock(inputBuffer, 0, encryptedData.Length);
        }
        finally
        {
            _arrayPool.Return(inputBuffer);
        }
    }

    /// <summary>
    /// 데이터 암호화 (ArrayPool 사용 - 최적화 버전)
    /// 반환된 버퍼는 반드시 ArrayPool.Shared.Return()으로 반환해야 함
    /// </summary>
    /// <returns>(암호화된 버퍼, 실제 암호화 데이터 길이)</returns>
    public static (byte[] Buffer, int Length) EncryptToPool(ReadOnlySpan<byte> data, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();

        var inputBuffer = _arrayPool.Rent(data.Length);
        try
        {
            data.CopyTo(inputBuffer);
            var encrypted = encryptor.TransformFinalBlock(inputBuffer, 0, data.Length);

            // 암호화된 데이터를 ArrayPool 버퍼로 복사
            var outputBuffer = _arrayPool.Rent(encrypted.Length);
            encrypted.CopyTo(outputBuffer, 0);

            return (outputBuffer, encrypted.Length);
        }
        finally
        {
            _arrayPool.Return(inputBuffer);
        }
    }

    /// <summary>
    /// 데이터 복호화 (ArrayPool 사용 - 최적화 버전)
    /// 반환된 버퍼는 반드시 ArrayPool.Shared.Return()으로 반환해야 함
    /// </summary>
    /// <returns>(복호화된 버퍼, 실제 복호화 데이터 길이)</returns>
    public static (byte[] Buffer, int Length) DecryptToPool(ReadOnlySpan<byte> encryptedData, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();

        var inputBuffer = _arrayPool.Rent(encryptedData.Length);
        try
        {
            encryptedData.CopyTo(inputBuffer);
            var decrypted = decryptor.TransformFinalBlock(inputBuffer, 0, encryptedData.Length);

            // 복호화된 데이터를 ArrayPool 버퍼로 복사
            var outputBuffer = _arrayPool.Rent(decrypted.Length);
            decrypted.CopyTo(outputBuffer, 0);

            return (outputBuffer, decrypted.Length);
        }
        finally
        {
            _arrayPool.Return(inputBuffer);
        }
    }
}