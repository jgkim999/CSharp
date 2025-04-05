using SQLite;

namespace Unity.Tools.Models.Sqlite;

public class AssetDirectory
{
    [PrimaryKey] public int Id { get; set; }
    [Indexed] public string Name { get; set; } = string.Empty;
}
