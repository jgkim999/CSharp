using SQLite;

namespace Unity.Tools.Models.Sqlite;

public class BuildCacheFile
{
    [Indexed] public int DirId { get; set; }
    [Indexed] public string Filename { get; set; } = string.Empty;
}
