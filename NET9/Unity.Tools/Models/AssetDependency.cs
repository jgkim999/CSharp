using SQLite;

namespace Unity.Tools.Models;

public class AssetDependency
{
    [PrimaryKey] public string Guid { get; set; } = string.Empty;
    [Indexed] public string DependencyGuid { get; set; } = string.Empty;
}
