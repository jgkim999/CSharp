using SQLite;

namespace Unity.Tools.Models;

public class BuildCacheDirectory
{
    [PrimaryKey] public int Id { get; set; }
    [Indexed] public string Name { get; set; } = string.Empty;
}
