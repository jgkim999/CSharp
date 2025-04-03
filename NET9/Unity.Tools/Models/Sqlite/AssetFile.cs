using SQLite;

namespace Unity.Tools.Models.Sqlite;

public class AssetFile
{
    [PrimaryKey] public string Guid { get; set; } = string.Empty;
    [Indexed] public int DirId { get; set; }
    [Indexed] public string Filename { get; set; } = string.Empty;
    public int DependencyCount { get; set; } = 0;
}
