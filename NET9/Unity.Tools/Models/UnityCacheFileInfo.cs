namespace Unity.Tools.Models;

public class UnityCacheFileInfo
{
    public int DirNum { get; set; }
    public string Filename { get; set; } = string.Empty;
    public long Length { get; set; } = 0;
    public DateTime CreationTimeUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastWriteTimeUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastAccessTimeUtc { get; set; } = DateTime.UtcNow;

    public string CreationTimeUtcStr => CreationTimeUtc.ToString("yyyy-MM-dd HH:mm:ss.fff");
    public string LastWriteTimeUtcStr => LastWriteTimeUtc.ToString("yyyy-MM-dd HH:mm:ss.fff");
    public string LastAccessTimeUtcStr => LastAccessTimeUtc.ToString("yyyy-MM-dd HH:mm:ss.fff");
}
