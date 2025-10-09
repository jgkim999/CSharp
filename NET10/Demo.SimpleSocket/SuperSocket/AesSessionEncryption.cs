using System.Buffers;
using System.Security.Cryptography;
using Demo.SimpleSocket.SuperSocket.Interfaces;

namespace Demo.SimpleSocket.SuperSocket;

/// <summary>
/// AES 암호화 구현 (256비트 키, CBC 모드, PKCS7 패딩)
/// </summary>
public partial class AesSessionEncryption : ISessionEncryption
{
    private readonly Aes _aes;
    private readonly ILogger<AesSessionEncryption> _logger;
    private bool _disposed;

    public byte[] Key => _aes.Key;
    public byte[] IV => _aes.IV;

    // LoggerMessage 소스 생성기 (고성능 로깅)
    [LoggerMessage(Level = LogLevel.Debug, Message = "[AES 암호화 초기화] KeySize: {KeySize}, IVSize: {IVSize}")]
    private partial void LogEncryptionInitialized(int keySize, int ivSize);

    [LoggerMessage(Level = LogLevel.Debug, Message = "[AES 암호화] 원본: {OriginalSize} 바이트 → 암호화: {EncryptedSize} 바이트")]
    private partial void LogEncryption(int originalSize, int encryptedSize);

    [LoggerMessage(Level = LogLevel.Debug, Message = "[AES 복호화] 암호화: {EncryptedSize} 바이트 → 원본: {DecryptedSize} 바이트")]
    private partial void LogDecryption(int encryptedSize, int decryptedSize);

    [LoggerMessage(Level = LogLevel.Debug, Message = "[AES 암호화 정리 완료]")]
    private partial void LogDisposed();

    public AesSessionEncryption(ILogger<AesSessionEncryption> logger)
    {
        _logger = logger;
        _aes = Aes.Create();
        _aes.KeySize = 256;  // 256비트 키
        _aes.Mode = CipherMode.CBC;
        _aes.Padding = PaddingMode.PKCS7;
        _aes.GenerateKey();
        _aes.GenerateIV();

        LogEncryptionInitialized(Key.Length, IV.Length);
    }

    /// <summary>
    /// 데이터 암호화 (ArrayPool 사용, Aes 인스턴스 재사용)
    /// 반환된 배열은 반드시 ArrayPool에 반환해야 함
    /// </summary>
    /// <returns>(암호화된 버퍼, 실제 데이터 길이)</returns>
    public (byte[] Buffer, int Length) Encrypt(ReadOnlySpan<byte> data, ArrayPool<byte> arrayPool)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AesSessionEncryption));
        }

        using var encryptor = _aes.CreateEncryptor();

        var inputBuffer = arrayPool.Rent(data.Length);
        byte[]? encrypted = null;
        try
        {
            data.CopyTo(inputBuffer);
            encrypted = encryptor.TransformFinalBlock(inputBuffer, 0, data.Length);

            // 암호화된 데이터를 ArrayPool 버퍼로 복사
            var outputBuffer = arrayPool.Rent(encrypted.Length);
            encrypted.CopyTo(outputBuffer, 0);

            LogEncryption(data.Length, encrypted.Length);

            return (outputBuffer, encrypted.Length);
        }
        finally
        {
            arrayPool.Return(inputBuffer);
            // TransformFinalBlock이 할당한 배열 해제 (메모리 누수 방지)
            encrypted = null;
        }
    }

    /// <summary>
    /// 데이터 복호화 (ArrayPool 사용, Aes 인스턴스 재사용)
    /// 반환된 배열은 반드시 ArrayPool에 반환해야 함
    /// </summary>
    /// <returns>(복호화된 버퍼, 실제 데이터 길이)</returns>
    public (byte[] Buffer, int Length) Decrypt(ReadOnlySpan<byte> encryptedData, ArrayPool<byte> arrayPool)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AesSessionEncryption));
        }

        using var decryptor = _aes.CreateDecryptor();

        var inputBuffer = arrayPool.Rent(encryptedData.Length);
        byte[]? decrypted = null;
        try
        {
            encryptedData.CopyTo(inputBuffer);
            decrypted = decryptor.TransformFinalBlock(inputBuffer, 0, encryptedData.Length);

            // 복호화된 데이터를 ArrayPool 버퍼로 복사
            var outputBuffer = arrayPool.Rent(decrypted.Length);
            decrypted.CopyTo(outputBuffer, 0);

            LogDecryption(encryptedData.Length, decrypted.Length);

            return (outputBuffer, decrypted.Length);
        }
        finally
        {
            arrayPool.Return(inputBuffer);
            // TransformFinalBlock이 할당한 배열 해제 (메모리 누수 방지)
            decrypted = null;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _aes?.Dispose();
            LogDisposed();
        }

        _disposed = true;
    }

    ~AesSessionEncryption()
    {
        Dispose(disposing: false);
    }
}