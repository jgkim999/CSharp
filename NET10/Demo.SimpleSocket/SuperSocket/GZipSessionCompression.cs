using System.Buffers;
using System.IO.Compression;
using Demo.SimpleSocket.SuperSocket.Interfaces;
using Microsoft.IO;

namespace Demo.SimpleSocket.SuperSocket;

/// <summary>
/// GZip 압축 구현
/// </summary>
public class GZipSessionCompression : ISessionCompression
{
    private static readonly RecyclableMemoryStreamManager MemoryStreamManager = new();
    private readonly ILogger<GZipSessionCompression> _logger;

    public GZipSessionCompression(ILogger<GZipSessionCompression> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// GZip을 사용하여 데이터 압축
    /// RecyclableMemoryStream을 사용하여 메모리 할당 최소화
    /// </summary>
    /// <returns>(압축된 버퍼, 실제 압축 데이터 길이)</returns>
    public (byte[] Buffer, int Length) Compress(ReadOnlySpan<byte> data, ArrayPool<byte> arrayPool)
    {
        var originalSize = data.Length;

        using var output = MemoryStreamManager.GetStream("GZipCompression-Compress");
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
        {
            gzip.Write(data);
        }

        // RecyclableMemoryStream에서 버퍼를 가져와 ArrayPool 버퍼로 복사
        var compressedLength = (int)output.Length;
        var result = arrayPool.Rent(compressedLength);
        output.Position = 0;
        output.ReadExactly(result.AsSpan(0, compressedLength));

        _logger.LogDebug("[GZip 압축] 원본: {OriginalSize} 바이트 → 압축: {CompressedSize} 바이트 ({Ratio:F1}%)",
            originalSize, compressedLength, compressedLength * 100.0 / originalSize);

        return (result, compressedLength);
    }

    /// <summary>
    /// GZip으로 압축된 데이터 압축 해제
    /// RecyclableMemoryStream을 사용하여 메모리 할당 최소화
    /// </summary>
    public (byte[] Buffer, int Length) Decompress(ReadOnlySpan<byte> compressedData, ArrayPool<byte> arrayPool)
    {
        var compressedSize = compressedData.Length;

        try
        {
            using var input = MemoryStreamManager.GetStream("GZipCompression-Decompress-Input", compressedData);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = MemoryStreamManager.GetStream("GZipCompression-Decompress-Output");

            gzip.CopyTo(output);

            // RecyclableMemoryStream에서 버퍼를 가져와 ArrayPool 버퍼로 복사
            var decompressedLength = (int)output.Length;
            var result = arrayPool.Rent(decompressedLength);
            output.Position = 0;
            output.ReadExactly(result.AsSpan(0, decompressedLength));

            _logger.LogDebug("[GZip 압축 해제] 압축: {CompressedSize} 바이트 → 원본: {OriginalSize} 바이트 ({Ratio:F1}%)",
                compressedSize, decompressedLength, compressedSize * 100.0 / decompressedLength);

            return (result, decompressedLength);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GZip 압축 해제 실패] CompressedSize: {CompressedSize}, 처음 16바이트: {FirstBytes}",
                compressedSize,
                compressedSize >= 16
                    ? BitConverter.ToString(compressedData.Slice(0, 16).ToArray())
                    : BitConverter.ToString(compressedData.ToArray()));
            throw;
        }
    }
}