using SQLite;

namespace Unity.Tools.Models.Sqlite;

public class AssetDependency
{
    [Indexed] public string Guid { get; set; } = string.Empty;
    [Indexed] public string DependencyGuid { get; set; } = string.Empty;
}
