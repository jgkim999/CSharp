using SQLite;

namespace Unity.Tools.Models.Sqlite;

public class BuildCacheFile
{
    [Indexed] public int DirId { get; set; }
    [Indexed] public string Filename { get; set; } = string.Empty;
    [Indexed] public long Length { get; set; } = 0;
    [Indexed] public string CreationTimeUtc { get; set; } = DateTime.MinValue.ToString("yyyy-MM-dd HH:mm:ss.fff");
    [Indexed] public string LastWriteTimeUtc { get; set; } = DateTime.MinValue.ToString("yyyy-MM-dd HH:mm:ss.fff");
    [Indexed] public string LastAccessTimeUtc { get; set; } = DateTime.MinValue.ToString("yyyy-MM-dd HH:mm:ss.fff");
}

