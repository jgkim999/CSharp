using SQLite;

namespace Unity.Tools.Models;

public class AssetDirectory
{
    [PrimaryKey] public int Id { get; set; }
    [Unique] public string Name { get; set; } = string.Empty;
}
